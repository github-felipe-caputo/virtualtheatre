using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class BVH {

    public class Joint {
        public string name;
        public Vector3 offset;
        public int numChannels;
        public string[] orderChannel;

        public int channelOffset = 0; // where you should start looking for values for this joint

        public Joint parent;
        public Joint[] children;

        public KeyFrameAnimation ani; // every joint needs an animation object if we are using keyframes

        public void addKeyFrameAnimation(string str) {
            ani = new KeyFrameAnimation(str);
        }
    }

    public class Motion
    {
        public int numFrames;
        public int numTotalChannels = 0;
        public float frameTime;

        public float[] motionData;
    }

    Joint hips = new Joint();
    Motion motion = new Motion();
    int channelOffsetGlobal = 0;

    /*************** CONSTRUCTORS***************/
    public BVH(string file) {
        string bhv = file.Replace("\r\n", string.Empty);

        string motionString = file.Split('M')[1];

        bhv = bhv.Replace(" ", "");
        motionString = motionString.Replace(" ", "");
        motionString = motionString.Replace("\r\n", "|");

        bhv = removeFirstOcurrenceSubstring(bhv, "HIERARCHY");
        
        readHierarchy(bhv, motionString);
    }

    private void readHierarchy(string s, string motionString) {
        // might be root or motion
        if (s.Substring(0, 4).Equals("ROOT")) {
            s = removeFirstOcurrenceSubstring(s, "ROOT");

            // get name
            hips.name = s.Split('{')[0];
            s = s.Remove(0, s.IndexOf('{')+1);

            // get offset
            s = removeFirstOcurrenceSubstring(s, "\tOFFSET\t");
            hips.offset = new Vector3(float.Parse(s.Split('\t')[0]), float.Parse(s.Split('\t')[1]), float.Parse(s.Split('\t')[2]));
            
            // get channel num
            s = s.Remove(0, s.IndexOf('C'));
            s = removeFirstOcurrenceSubstring(s, "CHANNELS\t");
            hips.numChannels = int.Parse(s[0].ToString());
            s = s.Remove(0, 1);

            hips.channelOffset = 0;
            channelOffsetGlobal += hips.numChannels;
            motion.numTotalChannels += hips.numChannels;

            // get channels
            hips.orderChannel = new string[hips.numChannels];
            for (int i = 0; i < hips.numChannels; ++i) {
                s = removeFirstOcurrenceSubstring(s, "\t");
                hips.orderChannel[i] = s.Substring(0,9);
                s = s.Remove(0, 9);
            }

            // hips will have three children
            hips.parent = null;
            hips.children = new Joint[3];


            // LeftHip
            hips.children[0] = new Joint();
            s = readJoint(s, hips.children[0], hips);
            s = s.Remove(0, s.IndexOf('J')); // "move" to next joint

            // RightHip
            hips.children[1] = new Joint();
            s = readJoint(s, hips.children[1], hips);
            s = s.Remove(0, s.IndexOf('J')); // "move" to next joint

            // Chest (chest has 3 children)
            // call readHierarchy here again to deal with the chest, should be easier (but very dirty)
            readHierarchy(s, motionString);
        }
        else if (s.Substring(0, 5).Equals("JOINT"))
        {
            hips.children[2] = new Joint();

            hips.children[2].parent = new Joint();
            hips.children[2].parent = hips;

            s = readAllValues(s, hips.children[2]);

            // this is chest, it will have 3 children
            hips.children[2].children = new Joint[3];

            // LeftCollar
            hips.children[2].children[0] = new Joint();
            s = readJoint(s, hips.children[2].children[0], hips.children[2]);
            s = s.Remove(0, s.IndexOf('J')); // "move" to next joint

            // RightCollar
            hips.children[2].children[1] = new Joint();
            s = readJoint(s, hips.children[2].children[1], hips.children[2]);
            s = s.Remove(0, s.IndexOf('J')); // "move" to next joint

            // Neck
            hips.children[2].children[2] = new Joint();
            s = readJoint(s, hips.children[2].children[2], hips.children[2]);
            s = s.Remove(0, s.IndexOf('M')); // over, "move" to MOTION

            // now, read motion!
            readHierarchy(s, motionString);
        }
        else
        {
            readMotion(motionString);
        }
    }

    // will read a join and put it on Joint j
    // then it returns the bhv string without this joint
    private string readJoint(string s, Joint j, Joint parent)
    {
        while (s[0].Equals('\t')) {
            s = s.Remove(0, 1);
        }

        // stop condition of the recurrence
        if (s.Substring(0, 7).Equals("EndSite"))
        {
            s = removeFirstOcurrenceSubstring(s, "EndSite");
            j.name = "EndSite";
            s = s.Remove(0, s.IndexOf('{') + 1);

            // get offset (here because of the joints there might be more than one tab)
            s = removeFirstOcurrenceSubstring(s, "\tOFFSET");
            while (s[0].Equals('\t')) {
                s = s.Remove(0, 1);
            }
            j.offset = new Vector3(float.Parse(s.Split('\t')[0]), float.Parse(s.Split('\t')[1]), float.Parse(s.Split('\t')[2]));

            j.parent = new Joint();
            j.parent = parent;

            return s;
        }

        j.parent = new Joint();
        j.parent = parent;

        j.children = new Joint[1];
        j.children[0] = new Joint();

        s = readAllValues(s, j);

        return readJoint(s, j.children[0], j);
    }

    private void readMotion(string s) {
        int index;

        // motion frames
        s = s.Remove(0, s.IndexOf('\t') + 1);
        motion.numFrames = int.Parse(s.Split('|')[0]);

        // frame time
        s = s.Remove(0, s.IndexOf('\t') + 1);
        motion.frameTime = float.Parse(s.Split('|')[0]);
        s = s.Remove(0, s.IndexOf('|') + 1);

        s = s.Replace('|', '\t');

        // actual data
        motion.motionData = new float[motion.numFrames * motion.numTotalChannels];

        //motion.numFrames = 200; // test
        string[] aux = new string[motion.numFrames * motion.numTotalChannels];
        aux = s.Split('\t');

        for (int i = 0; i < motion.numFrames; ++i) {
            for (int j = 0; j < motion.numTotalChannels; ++j) {
                index = i * motion.numTotalChannels + j;
                motion.motionData[index] = float.Parse(aux[index]);
            }
        }
        
    }

    public Joint getRoot()
    {
        return hips;
    }

    public Motion getMotion()
    {
        return motion;
    }

    private string readAllValues(string s, Joint j) {
        s = removeFirstOcurrenceSubstring(s, "JOINT");
        while (s[0].Equals('\t')) {
            s = s.Remove(0, 1);
        }

        // get name
        j.name = s.Split('{')[0];
        j.name = j.name.Split('\t')[0];
        s = s.Remove(0, s.IndexOf('{') + 1);

        // get offset (here because of the joints there might be more than one tab)
        s = removeFirstOcurrenceSubstring(s, "\tOFFSET");
        while (s[0].Equals('\t'))
        {
            s = s.Remove(0, 1);
        }
        j.offset = new Vector3(float.Parse(s.Split('\t')[0]), float.Parse(s.Split('\t')[1]), float.Parse(s.Split('\t')[2]));

        // get channel num
        s = s.Remove(0, s.IndexOf('C'));
        s = removeFirstOcurrenceSubstring(s, "CHANNELS\t");
        j.numChannels = int.Parse(s[0].ToString());
        s = s.Remove(0, 1);

        // update total channels
        j.channelOffset = channelOffsetGlobal;
        channelOffsetGlobal += j.numChannels;
        motion.numTotalChannels += j.numChannels;

        // get channels
        j.orderChannel = new string[j.numChannels];
        for (int i = 0; i < j.numChannels; ++i)
        {
            s = removeFirstOcurrenceSubstring(s, "\t");
            j.orderChannel[i] = s.Substring(0, 9);
            s = s.Remove(0, 9);
        }

        return s;
    }

    private string removeFirstOcurrenceSubstring(string s, string substring) {
        int index = s.IndexOf(substring);
        return (index < 0) ? s : s.Remove(index, substring.Length);
    }
}
