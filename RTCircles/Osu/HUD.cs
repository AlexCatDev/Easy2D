﻿using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    public class HUD : Drawable
    {
        public class Firework : Drawable
        {
            public override Rectangle Bounds => new Rectangle(pos - size / 2f, size);

            private Vector2 pos;
            private Vector2 size = new Vector2(8,8);
            private SmoothFloat alpha = new SmoothFloat();
            private Vector2 velocity;
            private float scale;

            public Firework(Vector2 pos, float speed = 1000f)
            {
                alpha.Value = 0f;
                alpha.TransformTo(1f, 0.25f, EasingTypes.Out);
                alpha.TransformTo(0f, 0.5f, EasingTypes.Out);

                float theta = RNG.Next(0, MathF.PI * 2);
                this.pos = pos + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * 1f;

                scale = RNG.Next(20f, 100f) / 100f;

                velocity.X = MathF.Cos(theta) * speed * scale;
                velocity.Y = MathF.Sin(theta) * speed * scale;
            }

            public override void Render(Graphics g)
            {
                float r = MathF.Abs((float)Math.Sin(OsuContainer.SongPosition / 1000d)) * 5;
                float green = MathF.Abs((float)Math.Cos(OsuContainer.SongPosition / 1000d)) * 5;
                float b = MathF.Abs(r - green) * 5;
                Vector4 color = new Vector4(r + 1, green + 1, b + 1, alpha);
                g.DrawRectangleCentered(pos, size, color, Texture.WhiteCircle);
            }

            public override void Update(float delta)
            {
                pos += velocity * delta;
                alpha.Update(delta);

                if (Bounds.IntersectsWith(new Rectangle(0, 0, MainGame.WindowWidth, MainGame.WindowHeight)) == false)
                    IsDead = true;
            }
        }

        class HitURJudgement : Drawable
        {
            Vector2 pos;
            HitResult result;

            Vector4 color;

            Vector2 size = new Vector2(4, 60);

            public HitURJudgement(Vector2 pos, HitResult result)
            {
                this.pos = pos;
                this.result = result;

                switch (result)
                {
                    case HitResult.Max:
                        color = Colors.From255RGBA(54, 187, 230, 255) * 1.2f;
                        break;
                    case HitResult.Good:
                        color = Colors.From255RGBA(92, 226, 22, 255) * 1.2f;
                        break;
                    case HitResult.Meh:
                        color = Colors.From255RGBA(216, 175, 73, 255) * 1.2f;
                        break;
                    case HitResult.Miss:
                        color = new Vector4(1f, 0f, 0f, 1f);
                        break;
                    default:
                        break;
                }
            }

            public override Rectangle Bounds => throw new NotImplementedException();

            public override void Render(Graphics g)
            {
                g.DrawRectangleCentered(pos - size / 4f, size, color);
            }

            public override void Update(float delta)
            {
                color.W -= delta * 0.3f;
                color.W = MathUtils.Clamp(color.W, 0, 1f);

                if (color.W == 0)
                    IsDead = true;
            }
        }

        private float scale = 1f;
        private float scaleTime = 0.2f;

        private float scale2 = 0;
        private float scaleTime2 = 0.4f;

        private float unstableRateBarWidth => (float)OsuContainer.Beatmap.Window50 * 3f;

        private Rectangle unstableRateBar => new Rectangle(new Vector2(MainGame.WindowCenter.X - unstableRateBarWidth / 2f, 30), new Vector2(unstableRateBarWidth, 25f));

        public override Rectangle Bounds => throw new NotImplementedException();

        private List<HitURJudgement> hitURJudgements = new List<HitURJudgement>();

        public void AddHit(float time, HitResult result, Vector2 position, bool displayJudgement = true)
        {
            if (result != HitResult.Miss)
            {
                //Watafuk ppy :<
                //The Difficulty multiplier equals the old star rating. It can be calculated via the following formula:
                //difficultyMultiplier = Round((HP Drain + Circle Size + Overall Difficulty + Clamp(Hit object count / Drain time in seconds * 8, 0, 16)) / 38 * 5)    

                double difficultyMultiplier = 1.0;
                
                double modMultiplier = 1.0;

                //LMAOOOOOOOO
                OsuContainer.Score += (uint)result + (uint)(((double)result) * (OsuContainer.Combo * difficultyMultiplier * modMultiplier / 25.0));

                OsuContainer.Combo++;

                if (OsuContainer.Combo > OsuContainer.MaxCombo)
                    OsuContainer.MaxCombo = OsuContainer.Combo;

                scaleTime = 0f;
                scaleTime2 = 0;
            }

            
            if(displayJudgement)
                ScreenManager.GetScreen<OsuScreen>().Add(new HitJudgement(position, result));

            //Console.WriteLine(result);
            //Map the whole width where 0 will be the center because its dead on time
            float x = MathUtils.Map(time, (float)OsuContainer.Beatmap.Window50, -(float)OsuContainer.Beatmap.Window50, unstableRateBar.Left, unstableRateBar.Right);

            hitURJudgements.Add(new HitURJudgement(new Vector2(x, unstableRateBar.Center.Y), result));

            switch (result)
            {
                case HitResult.Max:
                    OsuContainer.Count300++;
                    break;
                case HitResult.Good:
                    OsuContainer.Count100++;
                    break;
                case HitResult.Meh:
                    OsuContainer.Count50++;
                    break;
                case HitResult.Miss:
                    if (OsuContainer.Combo > 20 && OsuContainer.MuteHitsounds == false)
                        Skin.ComboBreak.Play(true);

                    OsuContainer.Combo = 0;
                    OsuContainer.CountMiss++;
                    break;
                default:
                    break;
            }
        }

        private void drawComboText(Graphics g)
        {
            string comboText = $"{OsuContainer.Combo}x";

            float comboScale = 80;

            Vector4 beatFlash = new Vector4(0.69f, 0.69f, 0.69f, 0f) * (float)OsuContainer.BeatProgressKiai;

            Vector2 comboSize = Skin.ComboNumbers.Meassure(comboScale * scale2, comboText);
            Skin.ComboNumbers.Draw(g, new Vector2(0, MainGame.WindowHeight - comboSize.Y), comboScale * scale2, new Vector4(1f, 1f, 1f, 0.7f) + beatFlash, comboText);

            comboSize = Skin.ComboNumbers.Meassure(comboScale * scale, comboText);
            Skin.ComboNumbers.Draw(g, new Vector2(0, MainGame.WindowHeight - comboSize.Y), comboScale * scale, Colors.White + beatFlash, comboText);
        }
        
        private void drawURBar(Graphics g)
        {
            //draw ur bar
            float width50 = (float)MathUtils.Map(OsuContainer.Beatmap.Window50, 0, OsuContainer.Beatmap.Window50, 0, unstableRateBar.Width);
            float width100 = (float)MathUtils.Map(OsuContainer.Beatmap.Window100, 0, OsuContainer.Beatmap.Window50, 0, unstableRateBar.Width);
            float width300 = (float)MathUtils.Map(OsuContainer.Beatmap.Window300, 0, OsuContainer.Beatmap.Window50, 0, unstableRateBar.Width);

            g.DrawRectangleCentered(unstableRateBar.Position + new Vector2(unstableRateBar.Width / 2f, 0), new Vector2(width50, unstableRateBar.Height), Colors.From255RGBA(214, 175, 70, 255));
            g.DrawRectangleCentered(unstableRateBar.Position + new Vector2(unstableRateBar.Width / 2f, 0), new Vector2(width100, unstableRateBar.Height), Colors.From255RGBA(88, 226, 16, 255));
            g.DrawRectangleCentered(unstableRateBar.Position + new Vector2(unstableRateBar.Width / 2f, 0), new Vector2(width300, unstableRateBar.Height), Colors.From255RGBA(51, 190, 223, 255));

            for (int i = 0; i < hitURJudgements.Count; i++)
            {
                hitURJudgements[i].Update((float)MainGame.Instance.RenderDeltaTime);
                hitURJudgements[i].Render(g);
            }

            for (int i = hitURJudgements.Count - 1; i >= 0; i--)
            {
                if (hitURJudgements[i].IsDead)
                    hitURJudgements.RemoveAt(i);
            }
        }

        private double rollingScore;
        private double rollingAcc;
        public override void Render(Graphics g)
        {
            drawDifficultyGraph(g);

            drawURBar(g);

            drawComboText(g);

            string scoreText = $"{((int)Math.Round(rollingScore, 0, MidpointRounding.AwayFromZero)).ToString("00000000.##")}";
            Vector2 scoreSize = Skin.ScoreNumbers.Meassure(66, scoreText);

            Skin.ScoreNumbers.Draw(g, new Vector2(MainGame.WindowWidth - scoreSize.X, 0), 66, Colors.White, scoreText);

            string accText = $"{rollingAcc:F2}%";
            Vector2 accSize = Skin.ScoreNumbers.Meassure(38, accText);
            Skin.ScoreNumbers.Draw(g, new Vector2(MainGame.WindowWidth - accSize.X, scoreSize.Y), 38, Colors.White, accText);

            float endAngle = (float)MathUtils.Map(OsuContainer.SongPosition, 0, OsuContainer.Beatmap.HitObjects.Count == 0 ? 0 : OsuContainer.Beatmap.HitObjects[^1].BaseObject.EndTime, -90, 270);

            var col = Colors.LightGray;

            float radius = 16f;
            Vector2 piePos = new Vector2(MainGame.WindowWidth - accSize.X - radius - 4, scoreSize.Y + radius);

            g.DrawEllipse(piePos, -90, endAngle, radius, 0, col);
            g.DrawRectangleCentered(piePos, new Vector2(radius * 2.6f), Colors.White, Skin.CircularMetre);

            var ranking = OsuContainer.GetCurrentRanking();
            OsuTexture tex = null;
            switch (ranking)
            {
                case OsuContainer.Ranking.X:
                    tex = Skin.RankingX;
                    break;
                case OsuContainer.Ranking.XH:
                    tex = Skin.RankingXH;
                    break;
                case OsuContainer.Ranking.S:
                    tex = Skin.RankingS;
                    break;
                case OsuContainer.Ranking.SH:
                    tex = Skin.RankingSH;
                    break;
                case OsuContainer.Ranking.A:
                    tex = Skin.RankingA;
                    break;
                case OsuContainer.Ranking.B:
                    tex = Skin.RankingB;
                    break;
                case OsuContainer.Ranking.C:
                    tex = Skin.RankingC;
                    break;
                case OsuContainer.Ranking.D:
                    tex = Skin.RankingD;
                    break;
                default:
                    break;
            }

            piePos.X -= radius*2.8f;

            float beatProgress = (float)Interpolation.ValueAt(OsuContainer.BeatProgressKiai, 0f, 1f, 0f, 1f, EasingTypes.InOutSine);

            float brightness = 1f + 1f * beatProgress;

            float rankSize = radius * 3.8f;
            g.DrawRectangleCentered(piePos, new Vector2(rankSize * tex.Texture.Size.AspectRatio(), rankSize), new Vector4(brightness, brightness, brightness, 1f), tex.Texture);

            piePos.X -= rankSize * 0.8f;
            rankSize /= 1.4f - 0.25f * beatProgress;
            drawDancer(g, piePos, new Vector2(rankSize, rankSize * Skin.Star.Texture.Size.AspectRatio()));

            drawKeyOverlay(g);
        }


        private FrameBuffer strainFB = new FrameBuffer(1, 1);
        private bool shouldGenGraph = true;
        private void drawDifficultyGraph(Graphics g)
        {
            Vector2 position = new Vector2(0, 0);
            Vector2 size = new Vector2(MainGame.WindowWidth, 100);

            if (strainFB.Width != size.X || strainFB.Height != size.Y)
            {
                strainFB.Resize(size.X, size.Y);
                shouldGenGraph = true;
            }

            if (shouldGenGraph)
            {
                List<Vector2> graph = new List<Vector2>();

                foreach (var item in OsuContainer.Beatmap.DifficultyGraph)
                {
                    graph.Add(new Vector2(0, (float)item));
                }

                if (graph.Count == 0)
                    return;

                Utils.Log($"Generating strain graph framebuffer!", LogLevel.Info);

                
                g.DrawInFrameBuffer(strainFB, () =>
                {
                    graph = PathApproximator.ApproximateCatmull(graph);

                    var vertices = g.VertexBatch.GetTriangleStrip(graph.Count * 2);

                    int vertexIndex = 0;

                    int textureSlot = g.GetTextureSlot(null);

                    float stepX = size.X / graph.Count;

                    Vector4 bottomColor = new Vector4(0.75f, 0.75f, 0.75f, 1f);
                    Vector4 peakColor = new Vector4(1f, 1f, 1f, 1f);

                    Vector2 movingPos = position;

                    for (int i = 0; i < graph.Count; i++)
                    {
                            //float height = graph[i].Y.Map(0, 10, 0, size.Y);

                            float height = graph[i].Y.Map(0, 10000, 0, size.Y);

                            //Grundlinje
                            vertices[vertexIndex].TextureSlot = textureSlot;
                        vertices[vertexIndex].Color = bottomColor;
                        vertices[vertexIndex].Position = movingPos;

                        vertexIndex++;

                        movingPos.Y += height;

                            //TopLinje
                            vertices[vertexIndex].TextureSlot = textureSlot;
                        vertices[vertexIndex].Color = peakColor;
                        vertices[vertexIndex].Position = movingPos;

                        movingPos.Y -= height;
                        movingPos.X += stepX;

                        vertexIndex++;
                    }
                });

                shouldGenGraph = false;
                
            }
            g.DrawFrameBuffer(position, new Vector4(1f, 1f, 1f, 0.5f), strainFB);

            Vector2 poo = new Vector2(64 * Skin.Arrow.Size.AspectRatio(), 64) * MainGame.Scale;

            Vector2 songPosPos = new Vector2((float)OsuContainer.SongPosition.Map(OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime, OsuContainer.Beatmap.HitObjects[^1].BaseObject.StartTime, position.X, position.X + size.X), position.Y + poo.Y / 4);

            g.DrawRectangleCentered(songPosPos, poo, new Vector4(4f,4f,4f,1f), Skin.Arrow, null, false, 180);
            //g.DrawLine(songPosPos, new Vector2(songPosPos.X, songPosPos.Y + size.Y), Colors.Red, 5f);
        }

        private float dancerAlpha = 0f;
        public void drawDancer(Graphics g, Vector2 starPos, Vector2 starSize)
        {
            dancerAlpha = MathHelper.Lerp(dancerAlpha, OsuContainer.IsKiaiTimeActive ? 1f : 0f, 5f * (float)MainGame.Instance.DeltaTime);

            if (dancerAlpha <= 0.05f)
                return;

            float angle = (int)Math.Floor(OsuContainer.CurrentBeat) % 2 == 0 ? 15 : -15f;

            float beatProgressKiai = Interpolation.ValueAt(OsuContainer.BeatProgressKiai, 0f, 1f, 0f, 1f, EasingTypes.InOutSine);

            float rotation = MathUtils.Map(beatProgressKiai, 0f, 1f, -angle, angle);

            float brightness = 1f + beatProgressKiai;

            Vector4 color = new Vector4(brightness, brightness, brightness, dancerAlpha);

            g.DrawRectangleCentered(starPos, starSize, color, Skin.Star, null, false, rotation);
        }

        private Vector2 key1Size = new Vector2(50);
        private Vector4 key1Color = Colors.White;

        private Vector2 key2Size = new Vector2(50);
        private Vector4 key2Color = Colors.White;
        public void drawKeyOverlay(Graphics g)
        {
            Vector2 key1Position = new Vector2(MainGame.WindowWidth - key1Size.X, MainGame.WindowHeight / 2f);
            float padding = 4f * MainGame.Scale;
            Vector2 key2Postion = new Vector2(MainGame.WindowWidth - key2Size.X, MainGame.WindowHeight / 2f + key1Size.Y + padding);
            float fontScale = 0.5f * MainGame.Scale;

            string key1Text = OsuContainer.Key1.ToString();
            string key2Text = OsuContainer.Key2.ToString();

            g.DrawRectangle(key1Position, key1Size, key1Color);

            Vector2 textSize = Font.DefaultFont.MessureString(key1Text, fontScale);
            g.DrawString(key1Text, Font.DefaultFont, key1Position + key1Size / 2f - textSize / 2f, Colors.Black, fontScale);

            g.DrawRectangle(key2Postion, key2Size, key2Color);

            textSize = Font.DefaultFont.MessureString(key2Text, fontScale);
            g.DrawString(key2Text, Font.DefaultFont, key2Postion + key2Size / 2f - textSize / 2f, Colors.Black, fontScale);

            //g.DrawString(OsuContainer.GetCurrentRankingLetter(), Font.DefaultFont, Easy2D.Game.Input.MousePosition, Colors.White);
        }

        public override void Update(float delta)
        {
            rollingScore = Interpolation.Damp(rollingScore, OsuContainer.Score, 0.05, delta * 10f);
            rollingAcc = Interpolation.Damp(rollingAcc, OsuContainer.Accuracy * 100, 0.05, delta * 10f);

            scaleTime += delta;
            scaleTime = scaleTime.Clamp(0f, 0.2f);

            scale = Interpolation.ValueAt(scaleTime, 1.8f, 1.5f, 0, 0.2f, EasingTypes.OutQuart);

            scaleTime2 += delta;
            scaleTime2 = scaleTime2.Clamp(0f, 0.4f);

            scale2 = Interpolation.ValueAt(scaleTime2, 2.7f, 1.5f, 0, 0.4f, EasingTypes.OutQuart);

            float pressSize = 50f * MainGame.Scale;
            float size = 65f * MainGame.Scale;
            const float lerpSpeed = 32f;

            if (OsuContainer.Key1Down)
            {
                key1Size = Vector2.Lerp(key1Size, new Vector2(pressSize), delta * lerpSpeed);
                key1Color = Vector4.Lerp(key1Color, Colors.Pink, delta * lerpSpeed);
            }
            else
            {
                key1Size = Vector2.Lerp(key1Size, new Vector2(size), delta * lerpSpeed);
                key1Color = Vector4.Lerp(key1Color, Colors.White, delta * lerpSpeed);
            }

            if (OsuContainer.Key2Down)
            {
                key2Size = Vector2.Lerp(key2Size, new Vector2(pressSize), delta * lerpSpeed);
                key2Color = Vector4.Lerp(key2Color, Colors.Pink, delta * lerpSpeed);
            }
            else
            {
                key2Size = Vector2.Lerp(key2Size, new Vector2(size), delta * lerpSpeed);
                key2Color = Vector4.Lerp(key2Color, Colors.White, delta * lerpSpeed);
            }
        }

        public HUD()
        {
            OsuContainer.BeatmapChanged += () =>
            {
                shouldGenGraph = true;
            };
        }
    }
}
