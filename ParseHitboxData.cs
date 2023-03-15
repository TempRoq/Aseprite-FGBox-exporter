using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Globalization;
public class ParseHitboxData : MonoBehaviour
{
    private readonly string txtPath = "Assets/Resources/txtParse/";
    public Sprite[] allSprites;

   // private readonly string outputPath = "Assets/"
    public string fileName;

   // public void 
   public struct BoxData
    {
        public Vector2 size;
        public Vector2 offsetFromCenter;
        public Color color;

        public Hitbox ToHitbox()
        {
            Hitbox h = new Hitbox();
            h.offsetFromAnchor = offsetFromCenter;
            h.dimensions = size;
            return h;
        }

        public bool Equals(BoxData bd)
        {
            return size == bd.size && offsetFromCenter == bd.offsetFromCenter && color == bd.color;
        }

        public static BoxData CreateBoxData(List<int> _color, List<int> TL, List<int> BR, Vector2 spriteSize, float ppu)
        {
            Vector2 worldLocation = new Vector2((TL[0] + BR[0]) / 2f, spriteSize.y - ((BR[1] + TL[1]) / 2f)) / ppu;
            Vector2 worldSize = new Vector2(BR[0] - TL[0], BR[1] - TL[1]) / ppu;
            Color c = new Color(_color[0], _color[1], _color[2], _color[3]);
            return new BoxData
            {
                size = worldSize,
                offsetFromCenter = worldLocation,
                color = c
            };
        }
    }


    public HitboxCluster CreateCluster(Hitbox[] hs, float secs)
    {
        return new HitboxCluster()
        {
            hitboxes = hs,
            durationInFrames = (int)(secs * 60f)
        };
    }
    public Action CreateAction(HitboxCluster[] hbcs)
    {
        int sum = 0;
        for (int i = 0; i < hbcs.Length; i++)
        {
            sum += hbcs[i].durationInFrames;
        }
        return new Action
        {
            clusters = hbcs,
            attackDuration = sum
        };
    }
    public static List<int> GetIntFromCommas(string s)
    {
        List<int> ret = new List<int>();
        int starting = 0;
        for (int i = 0; i < s.Length; ++i)
        {
            if (s[i] == ',')
            {
                ret.Add(int.Parse(s.Substring(starting, i)));
                starting = i + 1;
            }
        }
        return ret;
    }
    public static int GetEndOfContainer(string s, int start)
    {
        char ending;
        switch (s[start])
        {
            case '[':
                ending = ']';
                break;
            case '<':
                ending = '>';
                break;
            case '(':
                ending = ')';
                break;
            case '{':
                ending = '}';
                break;
            default:
                return -1;
        }
        int i = start;
        int openBrace = 1;
        while (i < s.Length)
        {
            if (s[i] == s[start])
            {
                openBrace++;
            }
            if (s[i] == ending)
            {
                openBrace--;
                if (openBrace == 0)
                {
                    return i;
                }
            }
            i++;
        }
        return -1;
    }

    //Gets all boxes in a subset of a frame. EX: Returns all hurtboxes' box data from frame 3.
    public static BoxData[] ParseBoxes(string s, Sprite spr)
    {
        string[] splitted = s.Split(':');
        BoxData[] ret = new BoxData[splitted.Length];
        for (int i = 0; i < splitted.Length; i++)
        {
            int endOfColor = GetEndOfContainer(s, 0);
            int endOfBracket1 = GetEndOfContainer(s, endOfColor + 1);

            ret[i] = BoxData.CreateBoxData(
                GetIntFromCommas(s.Substring(1, endOfColor)), //Will get colors (in the example above: 106,190,48,255)
                GetIntFromCommas(s.Substring(endOfColor + 1, endOfColor)),
                GetIntFromCommas(s.Substring(endOfBracket1 + 2, s[^1])), //To account for the commas, add 2
                spr.rect.size, spr.pixelsPerUnit);
        }    
        return ret;
    }

    //Gets all boxes for all frames, and puts them into a list.
    //ret[i] = all boxdata on keyframe i.
    //ret[i][0] = all hitboxes on keyframe i.
    //ret[i][1] = all hurtboxes on keyframe i.
    //ret[i][2] = all pushboxes on keyframe i.
    //durations[i] = duration of keyframe i.
    public BoxData[][][] ParseLine(string s, int startingFrame, out int framesTraveled, out List<int> _durations, out string aniName)
    {
        List<int> durations = new();
        List<BoxData[][]> ret = new();
        string[] splitted = s.Split('|');
        aniName = splitted[0];
        int spriteFrame = 0;
        int i = 1;
        while (i < splitted.Length)
        {
            BoxData[][] bd = new BoxData[3][];
            durations.Add( Mathf.FloorToInt(60f * float.Parse(splitted[i], CultureInfo.InvariantCulture.NumberFormat)));
            bd[0] = ParseBoxes(splitted[i + 1], allSprites[startingFrame + spriteFrame]);
            bd[1] = ParseBoxes(splitted[i + 2], allSprites[startingFrame + spriteFrame]);
            bd[2] = ParseBoxes(splitted[i + 3], allSprites[startingFrame + spriteFrame]);
            ret.Add(bd);
            i += 4;
            spriteFrame += 1;
        }

        _durations = durations;
        framesTraveled = spriteFrame;

        return ret.ToArray();
    }




    /* ASSUMPTIONS:
    * Everything is in priority order
    * Durations either has or is tied for the longest length
    */


    public static AnimationClip BuildAnimationClipNoSprites(BoxData[][][] bd, List<int> duration)
    {
        int maxHurtBoxes = 0;
        int maxPushBoxes = 0;
        for (int i = 0; i < bd.Length; i++)
        {
            maxHurtBoxes = Mathf.Max(maxHurtBoxes, bd[i][1].Length);
            maxPushBoxes = Mathf.Max(maxPushBoxes, bd[i][2].Length);
        }

        List<AnimationCurve[]> HurtCurve = new();
        AnimationClip ani = new AnimationClip();

        for (int i = 0; i < maxHurtBoxes; i++)
        {
            BoxData[] tempBoxes = new BoxData[bd.Length];
            for (int j = 0; j < bd.Length; j++)
            {
                if (bd[j][1].Length > j)
                {
                    tempBoxes[j] = bd[j][1][i];
                }
                else {
                    tempBoxes[j] = new BoxData()
                    {
                        size = new Vector2(0f, 0f),
                        offsetFromCenter = new Vector2(0f, 0f),
                        color = Color.black
                    };
                }
            }
            AnimationCurve[] hurtCurves = BoxDataToCurves(tempBoxes, duration); //tempBoxes are a collection of values for a single box
            string path = "Hurtboxes/Hurt" + (i + 1);
            ani.SetCurve(path, typeof(BoxCollider2D), "Offset.x", hurtCurves[0]);
            ani.SetCurve(path, typeof(BoxCollider2D), "Offset.y", hurtCurves[1]);
            ani.SetCurve(path, typeof(BoxCollider2D), "Size.x", hurtCurves[2]);
            ani.SetCurve(path, typeof(BoxCollider2D), "Size.y", hurtCurves[3]);
        }
        for (int i = 0; i < maxPushBoxes; i++)
        {
            BoxData[] tempBoxes = new BoxData[bd.Length];
            for (int j = 0; j < bd.Length; j++)
            {
                if (bd[j][2].Length > j)
                {
                    tempBoxes[j] = bd[j][2][i];
                }
                else
                {
                    tempBoxes[j] = new BoxData()
                    {
                        size = new Vector2(0f, 0f),
                        offsetFromCenter = new Vector2(0f, 0f),
                        color = Color.black
                    };
                }
            }
            AnimationCurve[] pushCurves = BoxDataToCurves(tempBoxes, duration); //tempBoxes are a collection of values for a single box
            string path = "Pushboxes/Push" + (i + 1);
            ani.SetCurve(path, typeof(BoxCollider2D), "Offset.x", pushCurves[0]);
            ani.SetCurve(path, typeof(BoxCollider2D), "Offset.y", pushCurves[1]);
            ani.SetCurve(path, typeof(BoxCollider2D), "Size.x", pushCurves[2]);
            ani.SetCurve(path, typeof(BoxCollider2D), "Size.y", pushCurves[3]);
        }

        return ani;


    }
    public static AnimationCurve[] BoxDataToCurves(BoxData[] boxes, List<int> durationInFrames)
    {
        List<Keyframe> offX = new();
        List<Keyframe> offY = new();
        List<Keyframe> sizeX = new();
        List<Keyframe> sizeY = new();

        int totalDuration = 0;


        for (int i = 0; i < boxes.Length; i++)
        {
            if (!(i != 0 && offX[^1].value == boxes[i].offsetFromCenter.x))
            {
                offX.Add(new Keyframe()
                {
                    time = totalDuration / 60f,
                    value = boxes[i].offsetFromCenter.x
                });
            }
            if (!(i != 0 && offY[^1].value == boxes[i].offsetFromCenter.y))
            {
                offY.Add(new Keyframe()
                {
                    time = totalDuration / 60f,
                    value = boxes[i].offsetFromCenter.y
                });
            }
            if (!(i != 0 && sizeX[^1].value == boxes[i].size.x))
            {
                sizeX.Add(new Keyframe()
                {
                    time = totalDuration / 60f,
                    value = boxes[i].size.x
                });
            }
            if (!(i != 0 && sizeY[^1].value == boxes[i].size.y))
            {
                sizeY.Add(new Keyframe()
                {
                    time = totalDuration / 60f,
                    value = boxes[i].size.x
                });
            }

            totalDuration += durationInFrames[i];
        }

        Keyframe k = sizeX[^1];
        k.time = (totalDuration - 1) / 60f;
        sizeX.Add(k);
        Keyframe k1 = sizeY[^1];
        k1.time = (totalDuration - 1) / 60f;
        sizeY.Add(k1);
        Keyframe k2 = offX[^1];
        k2.time = (totalDuration - 1) / 60f;
        offX.Add(k2);
        Keyframe k3 = offY[^1];
        k3.time = (totalDuration - 1) / 60f;
        offY.Add(k3);

    


        AnimationCurve[] ret = new AnimationCurve[4];
        ret[0] = new AnimationCurve(offX.ToArray());
        ret[1] = new AnimationCurve(offY.ToArray());
        ret[2] = new AnimationCurve(sizeX.ToArray());
        ret[3] = new AnimationCurve(sizeY.ToArray());

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < ret[i].keys.Length; j++)
            {
                AnimationUtility.SetKeyBroken(ret[i], j, true);
                AnimationUtility.SetKeyLeftTangentMode(ret[i], j, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(ret[i], j, AnimationUtility.TangentMode.Constant);
                ret[i].keys[j].inTangent = 0f;
                ret[i].keys[j].outTangent = 0f;
            }
        }

        return ret;
    }

    public void CreateFiles(string filePath)
    {
        int currentFrame = 0;
       
        string[] allAnimations = System.IO.File.ReadAllLines(txtPath + fileName);
        AnimationClip[] clips = new AnimationClip[allAnimations.Length];
        List<Action> actions = new();
        for(int a = 0; a < allAnimations.Length; a++)
        {
            //First part builds animation
            List<int> dur = new();
            BoxData[][][] animationData = ParseLine(allAnimations[a], currentFrame, out int frameTrav, out dur, out string aniName);
            AnimationClip ani = BuildAnimationClipNoSprites(animationData, dur);
            ani.name = aniName;

            List<ObjectReferenceKeyframe> spriteKeys = new List<ObjectReferenceKeyframe>();

            int total = 0;
            for (int i = 0; i < frameTrav; i++)
            {
                ObjectReferenceKeyframe ork = new ObjectReferenceKeyframe()
                {
                    time = total / 60f,
                    value = allSprites[i + currentFrame]
                };
                total += dur[i];
                spriteKeys.Add(ork);
            }
            ObjectReferenceKeyframe or = spriteKeys[^1];
            or.time = (total - 1) / 60f;
            spriteKeys.Add(or);

            EditorCurveBinding b = new()
            {
                propertyName = "m_Sprite",
                path = "",
                type = typeof(SpriteRenderer)
            };
            AnimationUtility.SetObjectReferenceCurve(ani, b, spriteKeys.ToArray());
            currentFrame += frameTrav;
            clips[a] = ani;


            List<HitboxCluster> hcs = new();
            int frameTime = 0;
            //Second part checks to see if an action should be built. If so, builds an action
            for (int i = 0; i < animationData.Length; i++)
            {
                if (animationData[i][0].Length != 0)
                {
                    HitboxCluster hc = new HitboxCluster();
                    List<Hitbox> hs = new();
                    for (int j = 0; j < animationData[i][0].Length; j++)
                    {
                        hs.Add(animationData[i][0][j].ToHitbox());
                    }
                    hcs.Add( new HitboxCluster()
                    {
                        hitboxes = hs.ToArray(),
                        startFrame = frameTime,
                        durationInFrames = dur[i]
                    });
                    

                }
                frameTime += dur[i];

            }
            if (hcs.Count > 0)
            {
                actions.Add(new Action()
                {
                    clusters = hcs.ToArray(),
                    attackDuration = frameTime,
                }) ;
            }
        }
    }
}
