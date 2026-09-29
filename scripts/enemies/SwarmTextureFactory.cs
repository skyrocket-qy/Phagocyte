using Godot;

namespace Game.Enemies;

/// <summary>
/// Procedural texture baker for microscopic swarm enemy MultiMesh instances.
/// Generates biological envelope, capsid, and flagella pixel art textures at runtime.
/// </summary>
public static class SwarmTextureFactory
{
    public static Texture2D CreateTexture(string speciesId) => speciesId switch
    {
        "norovirus" => BuildNorovirusTexture(),
        "flu_drift" => BuildFluDriftTexture(),
        "tb" => BuildTbTexture(),
        "e_coli" => BuildEColiTexture(),
        "staph" => BuildStaphTexture(),
        "staph_shielded" => BuildStaphShieldedTexture(),
        "pseudomonas" => BuildPseudomonasTexture(),
        "anthrax_bacillus" => BuildAnthraxTexture(),
        "plasmodium_merozoite" => BuildMerozoiteTexture(),
        "prion_fragment" => BuildPrionTexture(),
        "tachyzoite" => BuildTachyzoiteTexture(),
        "candida" => BuildCandidaTexture(),
        "toxoplasma" => BuildToxoplasmaTexture(),
        "hiv" => BuildHivTexture(),
        "rabies" => BuildRabiesTexture(),
        "varicella_zoster" => BuildVaricellaZosterTexture(),
        "malignant_cell" => BuildMalignantCellTexture(),
        "aspergillus" => BuildAspergillusTexture(),
        "tetanus" => BuildTetanusTexture(),
        "s_virus" => BuildSVirusTexture(),
        "plasmodium" => BuildPlasmodiumTexture(),
        "candida_retracted" => BuildCandidaRetractedTexture(),
        "varicella_dormant" => BuildVaricellaDormantTexture(),
        _ => throw new System.ArgumentException($"Unknown swarm species '{speciesId}'")
    };

    public static ImageTexture BuildNorovirusTexture()
    {
        const int size = 16;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float apothem = 5.6f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float hexDistance = -1e9f;
                for (int edge = 0; edge < 6; edge++)
                {
                    float angle = Mathf.DegToRad(30.0f + edge * 60.0f);
                    Vector2 normal = Vector2.FromAngle(angle);
                    hexDistance = Mathf.Max(hexDistance, p.Dot(normal));
                }

                Color color = new Color(0, 0, 0, 0);
                if (hexDistance <= apothem)
                    color = new Color(0.8f, 0.8f, 0.8f, 0.95f);
                if (p.Length() <= 3.0f)
                    color = new Color(1.0f, 1.0f, 1.0f, 0.98f);

                image.SetPixel(x, y, color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildFluDriftTexture()
    {
        const int size = 48;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = p.Length();

                Color color = new Color(0, 0, 0, 0);
                if (d <= 22.0f)
                    color = new Color(1.0f, 1.0f, 1.0f, 0.22f);
                if (d <= 14.0f)
                    color = new Color(0.82f, 0.82f, 0.82f, 0.95f);
                if (d <= 9.0f)
                    color = new Color(1.0f, 1.0f, 1.0f, 0.95f);

                image.SetPixel(x, y, color);
            }
        }

        for (int i = 0; i < 14; i++)
        {
            float angle = i * (Mathf.Tau / 14.0f);
            Vector2 basePos = Vector2.FromAngle(angle) * 13.0f;
            Vector2 tipPos = Vector2.FromAngle(angle) * 20.0f;
            bool even = i % 2 == 0;
            Color spike = even
                ? new Color(0.85f, 1.0f, 1.0f, 0.95f)
                : new Color(0.95f, 0.72f, 0.62f, 0.95f);

            DrawBakedLine(image, center + basePos, center + tipPos, spike, 2);
            if (!even)
                FillBakedCircle(image, center + tipPos, 2.2f, spike);
        }

        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildTbTexture()
    {
        const int size = 64;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        Vector2[] pts = { new(-24.6f, -6.6f), new(-9.9f, 3.3f), new(9.9f, 3.3f), new(24.6f, -6.6f) };
        var border = new Color(0.9f, 0.3f, 0.5f, 0.85f);
        var core = new Color(0.6f, 0.1f, 0.25f, 0.95f);
        for (int i = 0; i < pts.Length - 1; i++)
        {
            DrawBakedLine(image, center + pts[i], center + pts[i + 1], border, 13);
            DrawBakedLine(image, center + pts[i], center + pts[i + 1], core, 7);
        }
        FillBakedCircle(image, center + new Vector2(-13.2f, 0), 3.3f, Colors.White);
        FillBakedCircle(image, center + new Vector2(0, 3.3f), 3.3f, Colors.White);
        FillBakedCircle(image, center + new Vector2(13.2f, 0), 3.3f, Colors.White);
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildEColiTexture()
    {
        const int size = 72;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        var capsule = new Color(0.72f, 0.45f, 0.25f, 0.95f);
        var inner = new Color(0.90f, 0.62f, 0.38f, 0.90f);
        DrawBakedLine(image, center + new Vector2(-17.3f, 0), center + new Vector2(17.3f, 0), capsule, 16);
        FillBakedCircle(image, center + new Vector2(17.3f, 0), 8.0f, capsule);
        FillBakedCircle(image, center + new Vector2(-17.3f, 0), 8.0f, capsule);
        DrawBakedLine(image, center + new Vector2(-13.3f, 0), center + new Vector2(13.3f, 0), inner, 10);
        var hair = new Color(0.85f, 0.65f, 0.45f, 0.6f);
        for (int i = 0; i < 8; i++)
        {
            float ang = i * (Mathf.Tau / 8.0f);
            Vector2 root = center + new Vector2(Mathf.Cos(ang) * 18.7f, Mathf.Sin(ang) * 9.3f);
            Vector2 tip = root + Vector2.FromAngle(ang) * 16.0f;
            DrawBakedLine(image, root, tip, hair, 1);
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static void BakeStaphCluster(Image image, Vector2 center, Color baseGolden)
    {
        Vector2[] offs = { new(-7, -6), new(7, -5), new(-2, 6), new(6, 7) };
        float[] rads = { 8.0f, 7.5f, 8.5f, 7.0f };
        for (int i = 0; i < offs.Length; i++)
        {
            Vector2 pos = center + offs[i];
            float rad = rads[i];
            FillBakedCircle(image, pos, rad + 1.6f, new Color(0.65f, 0.42f, 0.05f, 0.85f));
            FillBakedCircle(image, pos, rad, baseGolden);
            FillBakedCircle(image, pos + new Vector2(-rad * 0.15f, -rad * 0.15f), rad * 0.72f, baseGolden.Lightened(0.18f));
            FillBakedCircle(image, pos + new Vector2(-rad * 0.32f, -rad * 0.32f), rad * 0.28f, new Color(1, 1, 1, 0.85f));
        }
    }

    public static ImageTexture BuildStaphTexture()
    {
        const int size = 40;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        BakeStaphCluster(image, new Vector2(size * 0.5f, size * 0.5f), new Color(0.95f, 0.78f, 0.18f, 0.95f));
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildStaphShieldedTexture()
    {
        const int size = 48;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        BakeStaphCluster(image, center, new Color(0.95f, 0.78f, 0.18f, 0.95f));
        var ring = new Color(1.0f, 0.95f, 0.7f, 0.7f);
        for (int i = 0; i < 24; i++)
        {
            float ang = i * (Mathf.Tau / 24.0f);
            FillBakedCircle(image, center + Vector2.FromAngle(ang) * 18.0f, 1.5f, ring);
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildPseudomonasTexture()
    {
        const int size = 48;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        var capsule = new Color(0.15f, 0.65f, 0.45f, 0.95f);
        var core = new Color(0.25f, 0.85f, 0.60f, 0.90f);
        DrawBakedLine(image, center + new Vector2(-18.7f, 0), center + new Vector2(18.7f, 0), capsule, 19);
        FillBakedCircle(image, center + new Vector2(18.7f, 0), 9.3f, capsule);
        FillBakedCircle(image, center + new Vector2(-18.7f, 0), 9.3f, capsule);
        DrawBakedLine(image, center + new Vector2(-14.7f, 0), center + new Vector2(14.7f, 0), core, 10);
        DrawBakedLine(image, center + new Vector2(-10.7f, -4), center + new Vector2(10.7f, -4), new Color(1, 1, 1, 0.7f), 2);
        Vector2 prev = center + new Vector2(-18.7f, 0);
        var flag = new Color(0.3f, 0.8f, 0.5f, 0.65f);
        for (int i = 1; i <= 6; i++)
        {
            float wave = Mathf.Sin(i * 0.8f) * 6.0f;
            Vector2 next = center + new Vector2(-18.7f - i * 6.7f, wave);
            DrawBakedLine(image, prev, next, flag, 2);
            prev = next;
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildMalignantCellTexture()
    {
        const int size = 96;
        const float scale = 37.0f / 34.0f;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        var cytoplasm = new Color(0.32f, 0.12f, 0.18f, 0.85f);
        var membrane = new Color(0.75f, 0.22f, 0.32f, 0.95f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float ang = Mathf.Atan2(p.Y, p.X);
                if (ang < 0.0f)
                    ang += Mathf.Tau;
                float vi = ang / Mathf.Tau * 24.0f;
                float edge = (32.0f + Mathf.Sin(vi * 1.5f) * 4.5f + Mathf.Cos(vi * 2.1f) * 3.0f) * scale;
                float d = p.Length();

                Color color = new Color(0, 0, 0, 0);
                if (d <= edge)
                    color = cytoplasm;
                if (Mathf.Abs(d - edge) <= 1.4f)
                    color = membrane;

                image.SetPixel(x, y, color);
            }
        }

        var nucleusCol = new Color(0.18f, 0.05f, 0.22f, 0.95f);
        var chromatinCol = new Color(0.55f, 0.15f, 0.65f, 0.90f);
        Vector2[] nuclei = { new(-10, -6), new(8, -8), new(2, 9) };
        foreach (var np in nuclei)
        {
            FillBakedCircle(image, center + np * scale, 9.0f * scale, nucleusCol);
            FillBakedCircle(image, center + np * scale, 5.5f * scale, chromatinCol);
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildAspergillusTexture()
    {
        const int size = 64;
        const float scale = 21.0f / 17.0f;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        DrawBakedLine(image, center + new Vector2(0, 8 * scale), center + new Vector2(0, 22 * scale),
            new Color(0.28f, 0.42f, 0.22f, 0.95f), Mathf.Max(1, (int)Mathf.Round(5.0f * scale)));
        FillBakedCircle(image, center + new Vector2(0, 6 * scale), 9.0f * scale,
            new Color(0.35f, 0.55f, 0.25f, 0.95f));
        var sporeCol = new Color(0.18f, 0.32f, 0.15f, 0.95f);
        var tipCol = new Color(0.45f, 0.85f, 0.30f, 0.95f);
        const int chains = 9;
        for (int i = 0; i < chains; i++)
        {
            float ang = Mathf.Pi * 0.85f + (i / (float)(chains - 1)) * Mathf.Pi * 1.3f;
            Vector2 dir = Vector2.FromAngle(ang);
            for (int s = 1; s <= 3; s++)
            {
                Vector2 pos = center + new Vector2(0, 6 * scale) + dir * ((9.0f + s * 4.5f) * scale);
                FillBakedCircle(image, pos, 2.8f * scale, s == 3 ? tipCol : sporeCol);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildTetanusTexture()
    {
        const int size = 64;
        const float scale = 21.0f / 15.0f;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        DrawBakedLine(image, center + new Vector2(-16 * scale, 0), center + new Vector2(6 * scale, 0),
            new Color(0.35f, 0.28f, 0.45f, 0.95f), Mathf.Max(1, (int)Mathf.Round(6.0f * scale)));
        FillBakedCircle(image, center + new Vector2(10 * scale, 0), 8.5f * scale,
            new Color(0.65f, 0.55f, 0.78f, 0.95f));
        FillBakedCircle(image, center + new Vector2(10 * scale, 0), 5.5f * scale,
            new Color(0.85f, 0.75f, 0.95f, 0.9f));
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildSVirusTexture()
    {
        const int size = 32;
        const float scale = 8.0f / 15.0f;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        FillBakedCircle(image, center, 12.0f * scale, new Color(0.68f, 0.18f, 0.28f, 0.95f));
        FillBakedCircle(image, center, 8.0f * scale, new Color(0.88f, 0.35f, 0.45f, 0.95f));
        var spikeStem = new Color(0.95f, 0.4f, 0.4f, 0.95f);
        var spikeCrown = new Color(1.0f, 0.65f, 0.2f, 1.0f);
        const int spikeCount = 12;
        for (int i = 0; i < spikeCount; i++)
        {
            float ang = i * (Mathf.Tau / spikeCount);
            Vector2 basePos = center + Vector2.FromAngle(ang) * (11.0f * scale);
            Vector2 headPos = center + Vector2.FromAngle(ang) * (18.0f * scale);
            DrawBakedLine(image, basePos, headPos, spikeStem, 1);
            FillBakedCircle(image, headPos, 2.8f * scale, spikeCrown);
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildPlasmodiumTexture()
    {
        const int size = 72;
        const float scale = 29.0f / 18.0f;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        var rbcCenter = new Color(0.55f, 0.12f, 0.12f, 0.95f);
        FillBakedCircle(image, center, 17.0f * scale, new Color(0.78f, 0.18f, 0.18f, 0.95f));
        FillBakedCircle(image, center, 11.0f * scale, rbcCenter);
        var parasiteCol = new Color(0.35f, 0.15f, 0.55f, 0.95f);
        var chromatinDot = new Color(0.95f, 0.25f, 0.35f, 1.0f);
        FillBakedCircle(image, center + new Vector2(-3, -2) * scale, 4.5f * scale, parasiteCol);
        FillBakedCircle(image, center + new Vector2(-3, -2) * scale, 2.5f * scale, rbcCenter);
        FillBakedCircle(image, center + new Vector2(-1, -4) * scale, 1.8f * scale, chromatinDot);
        FillBakedCircle(image, center + new Vector2(4, 3) * scale, 3.5f * scale, parasiteCol);
        FillBakedCircle(image, center + new Vector2(5, 2) * scale, 1.5f * scale, chromatinDot);
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildCandidaTexture()
    {
        const int size = 96;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float s = 26.0f / 16.0f;
        var yeastWall = new Color(0.88f, 0.85f, 0.72f, 0.95f);
        var yeastCore = new Color(0.98f, 0.95f, 0.85f, 0.95f);
        FillBakedCircle(image, center, 12.0f * s, yeastWall);
        FillBakedCircle(image, center, 9.0f * s, yeastCore);
        Vector2 bud = center + new Vector2(10.0f * s, -8.0f * s);
        FillBakedCircle(image, bud, 6.0f * s, yeastWall);
        FillBakedCircle(image, bud, 4.5f * s, yeastCore);
        var hyphaeCol = new Color(0.65f, 0.2f, 0.25f, 0.95f);
        for (int i = 0; i < 6; i++)
        {
            float ang = i * (Mathf.Tau / 6.0f) + 0.3f;
            Vector2 basePos = center + Vector2.FromAngle(ang) * 11.0f * s;
            Vector2 tipPos = center + Vector2.FromAngle(ang) * 28.0f * s;
            DrawBakedLine(image, basePos, tipPos, hyphaeCol, 5);
            FillBakedCircle(image, tipPos, 2.5f * s, Colors.Crimson);
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildToxoplasmaTexture()
    {
        const int size = 64;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float s = 25.0f / 16.0f;
        var bodyColor = new Color(0.45f, 0.25f, 0.65f, 0.95f);
        var coreColor = new Color(0.75f, 0.45f, 0.95f, 0.95f);
        var conoidColor = new Color(1.0f, 0.7f, 0.3f, 1.0f);
        Vector2[] local = { new(-12, -8), new(-4, -14), new(8, -10), new(14, 0), new(10, 10), new(2, 6), new(-4, -2) };
        Vector2[] poly = new Vector2[local.Length];
        for (int i = 0; i < local.Length; i++)
            poly[i] = center + local[i] * s;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                bool inside = false;
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    if ((poly[i].Y > p.Y) != (poly[j].Y > p.Y) &&
                        p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
                        inside = !inside;
                }
                if (inside)
                    image.SetPixel(x, y, bodyColor);
            }
        }
        FillBakedCircle(image, center + new Vector2(2.0f * s, -2.0f * s), 4.5f * s, coreColor);
        FillBakedCircle(image, center + new Vector2(-12.0f * s, -8.0f * s), 3.0f * s, conoidColor);
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildHivTexture()
    {
        const int size = 32;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float s = 9.0f / 14.0f;
        var envelopeColor = new Color(0.25f, 0.15f, 0.35f, 0.95f);
        var capsidColor = new Color(0.45f, 0.30f, 0.60f, 1.0f);
        FillBakedCircle(image, center, 12.0f * s, envelopeColor);
        Vector2[] local = { new(-6, -3), new(6, -5), new(7, 5), new(-6, 3) };
        Vector2[] poly = new Vector2[local.Length];
        for (int i = 0; i < local.Length; i++)
            poly[i] = center + local[i] * s;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                bool inside = false;
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    if ((poly[i].Y > p.Y) != (poly[j].Y > p.Y) &&
                        p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
                        inside = !inside;
                }
                if (inside)
                    image.SetPixel(x, y, capsidColor);
            }
        }
        var gp120Color = new Color(0.2f, 0.85f, 0.95f, 0.95f);
        for (int i = 0; i < 10; i++)
        {
            float ang = i * (Mathf.Tau / 10.0f);
            Vector2 basePos = center + Vector2.FromAngle(ang) * 11.0f * s;
            Vector2 headPos = center + Vector2.FromAngle(ang) * 16.0f * s;
            DrawBakedLine(image, basePos, headPos, gp120Color, 2);
            FillBakedCircle(image, headPos, 2.0f * s, gp120Color);
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildRabiesTexture()
    {
        const int size = 40;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float s = 10.0f / 13.0f;
        var envelopeCol = new Color(0.95f, 0.55f, 0.12f, 0.95f);
        var rnpCoreCol = new Color(1.0f, 0.85f, 0.25f, 0.95f);
        Vector2 trunkMin = center + new Vector2(-10.0f * s, -6.0f * s);
        Vector2 trunkMax = center + new Vector2(2.0f * s, 6.0f * s);
        for (int y = (int)Mathf.Floor(trunkMin.Y); y <= (int)Mathf.Ceil(trunkMax.Y); y++)
        {
            for (int x = (int)Mathf.Floor(trunkMin.X); x <= (int)Mathf.Ceil(trunkMax.X); x++)
            {
                if (x >= 0 && y >= 0 && x < size && y < size)
                    image.SetPixel(x, y, envelopeCol);
            }
        }
        FillBakedCircle(image, center + new Vector2(2.0f * s, 0), 6.0f * s, envelopeCol);
        DrawBakedLine(image, center + new Vector2(-10.0f * s, -6.0f * s), center + new Vector2(-10.0f * s, 6.0f * s), envelopeCol.Darkened(0.2f), 2);
        for (int x = -8; x <= 0; x += 3)
            DrawBakedLine(image, center + new Vector2(x * s, -4.0f * s), center + new Vector2(x * s, 4.0f * s), rnpCoreCol, 1);
        for (int y = -6; y <= 6; y += 4)
            DrawBakedLine(image, center + new Vector2(-10.0f * s, y * s), center + new Vector2(-13.0f * s, y * s), envelopeCol, 1);
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildVaricellaZosterTexture()
    {
        const int size = 40;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float s = 10.0f / 14.0f;
        var envelopeCol = new Color(0.85f, 0.15f, 0.15f, 0.95f);
        var coreCol = new Color(1.0f, 0.4f, 0.2f, 0.95f);
        FillBakedCircle(image, center, 13.0f * s, envelopeCol);
        FillBakedCircle(image, center, 8.0f * s, coreCol);
        for (int i = 0; i < 8; i++)
        {
            float ang = i * (Mathf.Tau / 8.0f);
            DrawBakedLine(image, center + Vector2.FromAngle(ang) * 12.0f * s, center + Vector2.FromAngle(ang) * 17.0f * s, envelopeCol, 2);
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildAnthraxTexture()
    {
        const int size = 72;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float scale = 25.0f / 14.0f;
        var capsule = new Color(0.42f, 0.28f, 0.48f, 0.95f);
        var core = new Color(0.68f, 0.45f, 0.75f, 1.0f);
        var boundary = new Color(0.85f, 0.7f, 0.95f, 0.45f);
        float hx = 14.0f * scale;
        float hy = 7.0f * scale;
        float ix = 11.0f * scale;
        float iy = 4.0f * scale;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                Color color = new Color(0, 0, 0, 0);
                if (Mathf.Abs(p.X) <= hx && Mathf.Abs(p.Y) <= hy)
                    color = capsule;
                if (Mathf.Abs(p.X) <= ix && Mathf.Abs(p.Y) <= iy)
                    color = core;
                image.SetPixel(x, y, color);
            }
        }
        Vector2 b0 = center + new Vector2(-16.0f * scale, -9.0f * scale);
        Vector2 b1 = center + new Vector2(16.0f * scale, -9.0f * scale);
        Vector2 b2 = center + new Vector2(16.0f * scale, 9.0f * scale);
        Vector2 b3 = center + new Vector2(-16.0f * scale, 9.0f * scale);
        DrawBakedLine(image, b0, b1, boundary, 3);
        DrawBakedLine(image, b1, b2, boundary, 3);
        DrawBakedLine(image, b2, b3, boundary, 3);
        DrawBakedLine(image, b3, b0, boundary, 3);
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildMerozoiteTexture()
    {
        const int size = 48;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float scale = 18.0f / 7.0f;
        FillBakedCircle(image, center, 6.0f * scale, new Color(0.75f, 0.22f, 0.35f, 0.95f));
        FillBakedCircle(image, center + new Vector2(0, -3.0f * scale), 2.5f * scale, new Color(1.0f, 0.4f, 0.5f, 1.0f));
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildPrionTexture()
    {
        const int size = 24;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float scale = 6.0f / 12.0f;
        Vector2[] pts = {
            center + new Vector2(-10, -8) * scale,
            center + new Vector2(2, -12) * scale,
            center + new Vector2(12, -2) * scale,
            center + new Vector2(8, 10) * scale,
            center + new Vector2(-6, 8) * scale
        };
        var crystal = new Color(0.35f, 0.12f, 0.55f, 0.95f);
        var edge = new Color(0.85f, 0.35f, 1.0f, 0.85f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    if ((pts[i].Y > p.Y) != (pts[j].Y > p.Y) &&
                        p.X < (pts[j].X - pts[i].X) * (p.Y - pts[i].Y) / (pts[j].Y - pts[i].Y) + pts[i].X)
                        inside = !inside;
                }
                if (inside)
                    image.SetPixel(x, y, crystal);
            }
        }
        for (int i = 0; i < pts.Length; i++)
            DrawBakedLine(image, pts[i], pts[(i + 1) % pts.Length], edge, 2);
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildTachyzoiteTexture()
    {
        const int size = 64;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float scale = 25.0f / 6.0f;
        Vector2 dir = Vector2.Right;
        Vector2 perp = dir.Orthogonal();
        DrawBakedLine(image, center - dir * 9.0f * scale, center + dir * 9.0f * scale, new Color(0.9f, 0.6f, 1.0f, 0.95f), 15);
        FillBakedCircle(image, center, 5.0f * scale, new Color(0.65f, 0.35f, 0.85f, 0.95f));
        FillBakedCircle(image, center + (-dir * 3.0f + perp * 2.0f) * scale, 1.6f * scale, new Color(1.0f, 0.75f, 0.35f, 1.0f));
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildCandidaRetractedTexture()
    {
        const int size = 96;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        FillBakedCircle(image, center, 19.5f, new Color(0.88f, 0.85f, 0.72f));
        FillBakedCircle(image, center, 14.6f, new Color(0.98f, 0.95f, 0.85f));
        Vector2 bud = center + new Vector2(16.25f, -13.0f);
        FillBakedCircle(image, bud, 9.75f, new Color(0.88f, 0.85f, 0.72f));
        FillBakedCircle(image, bud, 7.3f, new Color(0.98f, 0.95f, 0.85f));
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture BuildVaricellaDormantTexture()
    {
        const int size = 40;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        var envelope = new Color(0.4f, 0.4f, 0.5f, 0.2f);
        var core = new Color(0.6f, 0.6f, 0.7f, 0.2f);
        FillBakedCircle(image, center, 9.3f, envelope);
        FillBakedCircle(image, center, 5.7f, core);
        for (int i = 0; i < 8; i++)
        {
            float ang = i * (Mathf.Tau / 8.0f);
            DrawBakedLine(image, center + Vector2.FromAngle(ang) * 8.6f, center + Vector2.FromAngle(ang) * 12.1f, envelope, 2);
        }
        return ImageTexture.CreateFromImage(image);
    }

    public static void DrawBakedLine(Image image, Vector2 from, Vector2 to, Color color, int thickness)
    {
        float length = from.DistanceTo(to);
        int steps = Mathf.Max(1, (int)Mathf.Ceil(length));
        for (int s = 0; s <= steps; s++)
        {
            Vector2 point = from.Lerp(to, s / (float)steps);
            FillBakedCircle(image, point, thickness * 0.5f, color);
        }
    }

    public static void FillBakedCircle(Image image, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, (int)Mathf.Floor(center.X - radius));
        int maxX = Mathf.Min(image.GetWidth() - 1, (int)Mathf.Ceil(center.X + radius));
        int minY = Mathf.Max(0, (int)Mathf.Floor(center.Y - radius));
        int maxY = Mathf.Min(image.GetHeight() - 1, (int)Mathf.Ceil(center.Y + radius));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (new Vector2(x + 0.5f, y + 0.5f).DistanceTo(center) <= radius)
                    image.SetPixel(x, y, color);
            }
        }
    }
}
