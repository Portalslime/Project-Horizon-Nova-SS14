#!/usr/bin/env python3
"""
Prepares sound effects for the game: cuts, mixes down to mono, matches loudness and encodes OGG Vorbis.

Why mono: the engine cannot position stereo audio and every sound played on an entity is positional
(a stereo file asserts in debug builds and ignores the distance in release ones).
Why the loudness step: recordings differ by tens of dB, the game plays everything at the same base volume.

Usage:
    python Tools/_Horizon/audio/prepare_sfx.py              # every job
    python Tools/_Horizon/audio/prepare_sfx.py horse_eat_1  # only the named outputs
    python Tools/_Horizon/audio/prepare_sfx.py --dry-run    # print the numbers, write nothing

Needs: pip install soundfile numpy
The originals are not kept in the repository: put them into SFX_SOURCE_DIR (default: ~/Downloads).
To add a sound: add a Job below, run the script, then describe the result in the attributions.yml next to the output.
"""

import argparse
import os
import sys
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import soundfile as sf

REPO_ROOT = Path(__file__).resolve().parents[3]
OUTPUT_DIR = REPO_ROOT / "Resources" / "Audio" / "_Horizon" / "Animals"
SOURCE_DIR = Path(os.environ.get("SFX_SOURCE_DIR", Path.home() / "Downloads"))

# Loudness and silence are measured over short windows, a window counts as "the sound" when it is
# not further than this from the loudest one.
WINDOW_SECONDS = 0.02
ACTIVE_RANGE_DB = 30.0


@dataclass(frozen=True)
class Job:
    output: str  # file name without extension, written to OUTPUT_DIR
    source: str  # file name inside SOURCE_DIR
    start: float | None = None  # seconds, None = from the beginning
    end: float | None = None  # seconds, None = to the end
    trim_silence: bool = False  # cut the silence around the sound instead of giving start and end
    snap_start: bool = False  # move start to the quietest moment nearby, so the cut does not land in a sound
    snap_end: bool = False  # same for the end
    rms_db: float = -24.0  # loudness of the sound itself, dBFS
    ceiling_db: float = -3.0  # peaks are softly limited under this level, dBFS
    fade_in_ms: float = 5.0
    fade_out_ms: float = 40.0


EAT_SOURCE = "FOODEat_Horse eats carrot 3 (ID 1847)_BigSoundBank.com.wav"

JOBS = [
    # One 9 s recording of a horse chewing, cut into three clips of one series of bites each. The eating
    # do-after lasts about 2 s, and the three levels are matched so the variants do not jump in loudness.
    Job("horse_eat_1", EAT_SOURCE, start=0.07, end=1.65, snap_end=True, rms_db=-27.0),
    Job("horse_eat_2", EAT_SOURCE, start=1.65, end=3.10, snap_start=True, snap_end=True, rms_db=-27.0),
    Job("horse_eat_3", EAT_SOURCE, start=3.10, end=5.45, snap_start=True, snap_end=True, rms_db=-27.0),
]


def to_db(value: float) -> float:
    return 20.0 * np.log10(max(value, 1e-12))


def rms(x: np.ndarray) -> float:
    return float(np.sqrt(np.mean(x * x))) if x.size else 0.0


def load_mono(path: Path) -> tuple[np.ndarray, int]:
    data, sr = sf.read(path, dtype="float64", always_2d=True)
    mono = data.mean(axis=1)

    if data.shape[1] > 1:
        # Channels in opposite phase cancel each other in the mix, the sound gets thinner and quieter.
        loudest = max(rms(data[:, c]) for c in range(data.shape[1]))
        if rms(mono) < 0.7 * loudest:
            print(f"  warning: the channels partly cancel in the mix ({to_db(rms(mono) / loudest):.1f} dB), listen to the result")

    return mono, sr


def window_rms(x: np.ndarray, sr: int) -> np.ndarray:
    size = max(int(sr * WINDOW_SECONDS), 1)
    count = len(x) // size
    if count == 0:
        return np.array([rms(x)])

    return np.sqrt(np.mean(x[: count * size].reshape(count, size) ** 2, axis=1))


def active_rms_db(x: np.ndarray, sr: int) -> float:
    windows = window_rms(x, sr)
    active = windows >= windows.max() * 10 ** (-ACTIVE_RANGE_DB / 20)
    return to_db(float(np.sqrt(np.mean(windows[active] ** 2))))


def trim_silence(x: np.ndarray, sr: int, pad_start: float = 0.03, pad_end: float = 0.15, max_gap: float = 0.1) -> np.ndarray:
    windows = window_rms(x, sr)
    loud = np.flatnonzero(windows >= windows.max() * 10 ** (-ACTIVE_RANGE_DB / 20))

    # Isolated clicks and noise bursts must not stretch the clip: keep the group of windows with the most energy.
    groups = np.split(loud, np.flatnonzero(np.diff(loud) > max_gap / WINDOW_SECONDS) + 1)
    sound = max(groups, key=lambda group: float(np.sum(windows[group] ** 2)))

    size = int(sr * WINDOW_SECONDS)
    first = max(sound[0] * size - int(pad_start * sr), 0)
    last = min((sound[-1] + 1) * size + int(pad_end * sr), len(x))
    return x[first:last]


def quietest_point(x: np.ndarray, sr: int, t: float, radius: float = 0.15) -> float:
    """Time of the quietest window within radius of t."""
    size = max(int(sr * WINDOW_SECONDS), 1)
    centre = int(t * sr)
    lo = max(centre - int(radius * sr), 0)
    hi = min(centre + int(radius * sr), len(x) - size)
    if hi <= lo:
        return t

    energy = np.concatenate(([0.0], np.cumsum(x * x)))
    starts = np.arange(lo, hi)
    best = starts[np.argmin(energy[starts + size] - energy[starts])]
    return (best + size // 2) / sr


def cut(x: np.ndarray, sr: int, job: Job) -> np.ndarray:
    if job.trim_silence:
        return trim_silence(x, sr)

    start = job.start if job.start is not None else 0.0
    end = job.end if job.end is not None else len(x) / sr
    if job.snap_start:
        start = quietest_point(x, sr, start)
    if job.snap_end:
        end = quietest_point(x, sr, end)

    return x[int(start * sr): int(end * sr)]


def soft_limit(x: np.ndarray, ceiling: float, knee: float = 0.6) -> np.ndarray:
    """Leaves everything under ceiling * knee as it is and bends louder peaks smoothly towards the ceiling."""
    start = ceiling * knee
    over = np.abs(x) - start
    bent = start + (ceiling - start) * np.tanh(np.maximum(over, 0.0) / (ceiling - start))
    return np.where(over > 0.0, np.sign(x) * bent, x)


def match_loudness(x: np.ndarray, sr: int, rms_db: float, ceiling_db: float) -> np.ndarray:
    gain = 10 ** ((rms_db - active_rms_db(x, sr)) / 20)
    return soft_limit(x * gain, 10 ** (ceiling_db / 20))


def fade(x: np.ndarray, sr: int, in_ms: float, out_ms: float) -> np.ndarray:
    x = x.copy()
    n_in = min(int(sr * in_ms / 1000), len(x))
    n_out = min(int(sr * out_ms / 1000), len(x))
    if n_in:
        x[:n_in] *= np.sin(np.linspace(0.0, np.pi / 2, n_in)) ** 2
    if n_out:
        x[-n_out:] *= np.cos(np.linspace(0.0, np.pi / 2, n_out)) ** 2

    return x


def run(job: Job, dry_run: bool) -> bool:
    source = SOURCE_DIR / job.source
    if not source.exists():
        print(f"{job.output}: source not found: {source}")
        return False

    mono, sr = load_mono(source)
    clip = cut(mono, sr, job)
    if clip.size == 0:
        print(f"{job.output}: nothing left after the cut")
        return False

    out = fade(match_loudness(clip, sr, job.rms_db, job.ceiling_db), sr, job.fade_in_ms, job.fade_out_ms)
    report = f"{len(out) / sr:5.2f} s, peak {to_db(float(np.abs(out).max())):5.1f} dBFS, sound {active_rms_db(out, sr):5.1f} dBFS"

    if dry_run:
        print(f"{job.output}: {report} (dry run)")
        return True

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    target = OUTPUT_DIR / f"{job.output}.ogg"
    sf.write(target, out, sr, format="OGG", subtype="VORBIS")

    info = sf.info(target)  # read it back, so a broken file is noticed here and not in the game
    print(f"{job.output}: {report}, {target.stat().st_size / 1024:.0f} KB, {info.channels} ch {info.samplerate} Hz")
    return True


def main() -> int:
    parser = argparse.ArgumentParser(description="Cuts and encodes sound effects to mono OGG.")
    parser.add_argument("names", nargs="*", help="output names to process, all jobs by default")
    parser.add_argument("--dry-run", action="store_true", help="print the numbers and write nothing")
    args = parser.parse_args()

    jobs = [job for job in JOBS if not args.names or job.output in args.names]
    unknown = set(args.names) - {job.output for job in JOBS}
    if unknown:
        print(f"unknown outputs: {', '.join(sorted(unknown))}")
        return 1

    results = [run(job, args.dry_run) for job in jobs]
    return 0 if all(results) else 1


if __name__ == "__main__":
    sys.exit(main())
