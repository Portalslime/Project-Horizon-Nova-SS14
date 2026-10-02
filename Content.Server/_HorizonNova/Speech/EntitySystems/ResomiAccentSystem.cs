/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Акцент: после букв «ц» и «ч» ставится «щ».
 */

using System.Text;
using Content.Server._HorizonNova.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server._HorizonNova.Speech.EntitySystems;

public sealed class ResomiAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        var builder = new StringBuilder(message.Length);

        foreach (var c in message)
        {
            builder.Append(c);

            switch (c)
            {
                case 'ц':
                case 'Ц':
                case 'ч':
                case 'Ч':
                    builder.Append('щ');
                    break;
            }
        }

        return builder.ToString();
    }

    private void OnAccent(EntityUid uid, ResomiAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
