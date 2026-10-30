import os
import re
import sys

ROOT = r"F:\MyGames\LuaGitMy"
PROTO_DIR = os.path.join(ROOT, "Resources", "Prototypes", "_Arcane")
INTERACTIONS = os.path.join(PROTO_DIR, "Interactions", "ERP")

EXISTING_SPECIES = {
    "Arachnid", "Ariral", "Avali", "Demon", "Diona", "Dwarf", "Felinid",
    "Gingerbread", "Goblin", "Harpy", "Human", "Kobold", "Moth", "Monkey",
    "Oni", "Reptilian", "Resomi", "Shark", "Skeleton", "SlimePerson", "Vox",
    "Vulpkanin",
}

MISSING_SPECIES = {"HumanoidXeno", "Rodentia", "Tajaran"}

def process_blocks(lines):
    out = []
    i = 0
    n = len(lines)

    while i < n:
        line = lines[i]
        m = re.match(r"^(\s*)- !type:(SexRequirement|SpeciesRequirement)\s*$", line)
        if not m:
            out.append(line)
            i += 1
            continue

        indent = m.group(1)
        req_type = m.group(2)
        block = [line]
        i += 1

        # Collect the requirement block: following lines that are more indented
        # than the "- !type:" marker, until the next requirement/item at the same level.
        while i < n:
            nxt = lines[i]
            if nxt.strip() == "":
                block.append(nxt)
                i += 1
                continue

            cur_indent = len(nxt) - len(nxt.lstrip(" "))
            if cur_indent <= len(indent):
                break

            block.append(nxt)
            i += 1

        if req_type == "SexRequirement":
            block = [b for b in block if b.strip() != "- Futanari"]
            has_value = any(
                re.match(r"^\s+- \S", b) for b in block[1:]
            )
        else:
            filtered = []
            for b in block:
                sm = re.match(r"^(\s*)- (\S+)\s*$", b)
                if sm and sm.group(2) in MISSING_SPECIES:
                    continue
                filtered.append(b)
            block = filtered
            has_value = any(
                re.match(r"^\s+- \S", b) for b in block[1:]
            )

        if has_value:
            out.extend(block)

    return out


def process_file(path):
    with open(path, "r", encoding="utf-8-sig") as f:
        text = f.read()

    lines = text.splitlines()
    result = process_blocks(lines)

    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(result).rstrip() + "\n")


def main():
    if not os.path.isdir(INTERACTIONS):
        sys.exit("interaction dir missing")

    for name in sorted(os.listdir(INTERACTIONS)):
        if name.endswith(".yml"):
            process_file(os.path.join(INTERACTIONS, name))

    # Trim shader prototypes to the one that actually exists in this build.
    shaders = os.path.join(PROTO_DIR, "Shaders", "shaders.yml")
    if os.path.exists(shaders):
        with open(shaders, "w", encoding="utf-8") as f:
            f.write("""- type: shader
  id: ArousalScreenEffect
  kind: source
  path: "/Textures/_Arcane/Shaders/arousal_screen_effect.swsl"
""")

    # Tags used by ERP prototypes; entities simply will not have them unless
    # toy prototypes are ported, but the prototypes must load.
    tags_path = os.path.join(PROTO_DIR, "tags.yml")
    with open(tags_path, "w", encoding="utf-8") as f:
        for tag in ("SexToyAnal", "SexToyBlow", "SexToyButthole", "SexToyDildo",
                    "SexToyOnahole", "SexToyVibrator"):
            f.write(f"- type: tag\n  id: {tag}\n\n")

    # Validate icon/audio references.
    missing = []
    for name in sorted(os.listdir(INTERACTIONS)):
        if not name.endswith(".yml"):
            continue
        path = os.path.join(INTERACTIONS, name)
        with open(path, "r", encoding="utf-8-sig") as f:
            text = f.read()
        for m in re.finditer(r"icon:\s*(\S+)", text):
            rel = m.group(1)
            full = os.path.join(ROOT, "Resources", "Textures", *rel.split("/"))
            if not os.path.exists(full):
                missing.append(("icon", name, rel))
        for m in re.finditer(r"/Audio/_Arcane/Interactions/(\S+)", text):
            full = os.path.join(ROOT, "Resources", "Audio", "_Arcane", "Interactions", m.group(1))
            if not os.path.exists(full):
                missing.append(("audio", name, m.group(1)))

    print("missing:", len(missing))
    for item in missing[:20]:
        print(item)

if __name__ == "__main__":
    main()
