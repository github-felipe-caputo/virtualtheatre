using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

public class KinectBodyRecordedCardboard : MonoBehaviour {

    // bone material, necessary for colors
    public Material BoneMaterial;

    // Line renderer thickness
    public float LineThickness;

    // scaling the body
    // this scalesthe distance between the joints, it's encessary even when taking the parent's,
    // scaling into account
    public float scaleFactor;

    //
    // Values for reading the kinect data
    //

    // This is a ordered list of the kinect joints
    // e.g., joint 0 is SpineBase, joint 1 is SpineMid, etc...
    private List<string> jointMap = new List<string>()
    {
        {"SpineBase"}, {"SpineMid" }, {"Neck"}, {"Head"}, {"ShoulderLeft"}, {"ElbowLeft"},
        {"WristLeft"}, {"HandLeft"}, {"ShoulderRight"}, {"ElbowRight"}, {"WristRight"},
        {"HandRight"}, {"HipLeft"}, {"KneeLeft"}, {"AnkleLeft"}, {"FootLeft"}, {"HipRight"},
        {"KneeRight"}, {"AnkleRight"}, {"FootRight"}, {"SpineShoulder"}, {"HandTipLeft"},
        {"ThumbLeft"}, {"HandTipRight"}, {"ThumbRight"}
    };

    // joint parents to draw lines
    private Dictionary<string, string> boneMap = new Dictionary<string, string>()
    {
        { "FootLeft", "AnkleLeft" },
        { "AnkleLeft", "KneeLeft" },
        { "KneeLeft", "HipLeft" },
        { "HipLeft", "SpineBase" },

        { "FootRight", "AnkleRight" },
        { "AnkleRight", "KneeRight" },
        { "KneeRight", "HipRight" },
        { "HipRight", "SpineBase" },

        { "HandTipLeft", "HandLeft" },
        { "ThumbLeft", "HandLeft" },
        { "HandLeft", "WristLeft" },
        { "WristLeft", "ElbowLeft" },
        { "ElbowLeft", "ShoulderLeft" },
        { "ShoulderLeft", "SpineShoulder" },

        { "HandTipRight", "HandRight" },
        { "ThumbRight", "HandRight" },
        { "HandRight", "WristRight" },
        { "WristRight", "ElbowRight" },
        { "ElbowRight", "ShoulderRight" },
        { "ShoulderRight", "SpineShoulder" },

        { "SpineBase", "SpineMid" },
        { "SpineMid", "SpineShoulder" },
        { "SpineShoulder", "Neck" },
        { "Neck", "Head" },
    };

    // For the key frame, each joint needs a key frame object
    public class KinectJoint
    {
        // joint name
        public string name;

        // every joint needs an animation object if we are using keyframes
        public KeyFrameAnimation ani;

        public void addKeyFrameAnimation(string str) {
            ani = new KeyFrameAnimation(str);
        }
    }
    KinectJoint[] joints = new KinectJoint[25];

    public TextAsset filename;

    public float timeRate;
    public int frameRate;

    // linear interpolation or not?
    public bool interpolation;

    private int totalFrames;

    //
    // animation values
    //

    // animations happens between "current" and "next" keyframes
    private int keyFrameCurrent = 0;
    private int keyFrameNext = 1;

    private float timeFirstKeyFrame;
    private float timeLastKeyFrame;

    // Block man or stick figure?
    public bool StickMan;
    public bool BlockMan;
    public bool ParticleMan;
    public bool StarMan;

    public GameObject ParticleStarPrefab;
    public GameObject ParticleSystemPrefab;
    public GameObject CollisionPlane;

    //
    // UI Control variables
    //
    private bool playFlag = false;
    private float playTime = 0;
    private float pauseTime = 0;


    // Use this for initialization
    void Start()
    {
        if (BlockMan)
        {
            buildBodyBox(this.gameObject);
        }
        else if (ParticleMan)
        {
            buildBodyParticles(this.gameObject);
        }
        else if (StickMan)
        {
            buildBody(this.gameObject);
        }
        else if (StarMan)
        {
            buildBodyStars(this.gameObject);
        }

        // First thing is read the file and save the data as key frames
        sampleKinectData(filename.text, timeRate, frameRate);

        // Get first and last key frame times
        timeFirstKeyFrame = joints[0].ani.getTime(0);
        timeLastKeyFrame = joints[0].ani.getTime(joints[0].ani.getNumberOfKeyFrames() - 1);
    }

    // Update is called once per frame
    void Update()
    {
        float time = Time.time - playTime;
        int i = Time.frameCount;

        if (playFlag == true)
        {
            if (!interpolation)
            {
                if (i < totalFrames)
                {
                    moveBody(joints, i);
                }
            }
            else
            {
                // Linear
                if (time >= timeFirstKeyFrame && time <= timeLastKeyFrame)
                {
                    if (time < joints[0].ani.getTime(keyFrameNext)) // every joint will have the same time for next keyframes, let's just use 0 always
                    {
                        float u = (time - joints[0].ani.getTime(keyFrameCurrent)) / (joints[0].ani.getTime(keyFrameNext) - joints[0].ani.getTime(keyFrameCurrent));
                        moveBodyLinear(joints, keyFrameCurrent, keyFrameNext, u);
                    }
                    else
                    {
                        ++keyFrameCurrent;
                        ++keyFrameNext;
                    }
                }
            }
        }


        if (BlockMan)
        {
            refreshBodyBlock();
        }
        else if (ParticleMan || StarMan)
        {
            refreshBodyParticles();
        }
        else if (StickMan) {
            refreshBodyLines();
        }

        if (GameObject.Find("CardboardMain").GetComponent<Cardboard>().Triggered) {
            if (playFlag == false)
            {
                GameObject.Find("FoundTargetText").GetComponent<Text>().text = "";
                playTime = Time.time - (pauseTime - playTime);
                playFlag = true;
            }
            else
            {
                playFlag = false;
                pauseTime = Time.time;
            }
        }

        

    }

    void buildBody(GameObject parent)
    {
        int count = 0;

        foreach (string entry in jointMap)
        {
            // do something with entry.Value or entry.Key
            // assuming it has a line renderer
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jointObj.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");

            // no line rendered for the head
            if (!entry.Equals("Head"))
            {
                LineRenderer lr = jointObj.AddComponent<LineRenderer>();
                lr.SetVertexCount(2);
                lr.material = BoneMaterial;
                lr.SetWidth(LineThickness, LineThickness);
            }

            jointObj.transform.localScale = parent.transform.localScale / 2;
            jointObj.name = entry;
            jointObj.transform.parent = parent.transform;

            // now for the joint struct
            if (joints[count] == null)
            {
                joints[count] = new KinectJoint();
                joints[count].name = jointObj.name;
                count++;
            }

        }
    }

    void buildBodyParticles(GameObject parent)
    {
        int count = 0;

        foreach (string entry in jointMap)
        {
            // do something with entry.Value or entry.Key
            // assuming it has a line renderer
            GameObject jointObj = new GameObject();

            GameObject particles = (GameObject)Instantiate(ParticleSystemPrefab, jointObj.transform.position, Quaternion.identity);
            particles.GetComponent<ParticleSystem>().collision.SetPlane(1, CollisionPlane.transform);
            particles.transform.parent = jointObj.transform;

            jointObj.transform.localScale = parent.transform.localScale / 2;
            jointObj.name = entry;
            jointObj.transform.parent = parent.transform;

            // now for the joint struct
            if (joints[count] == null)
            {
                joints[count] = new KinectJoint();
                joints[count].name = jointObj.name;
                count++;
            }
        }

        // Extra particles 
        
        foreach (KeyValuePair<string, string> entry in boneMap)
        {
            GameObject jointObj = new GameObject();
            jointObj.name = entry.Value + "_" + entry.Key;
            jointObj.transform.parent = parent.transform;

            GameObject particles = (GameObject)Instantiate(ParticleSystemPrefab, Vector3.zero, Quaternion.identity);
            particles.GetComponent<ParticleSystem>().collision.SetPlane(1, CollisionPlane.transform);
            particles.transform.parent = jointObj.transform;
        }
        
    }

    void buildBodyBox(GameObject parent)
    {
        int count = 0;

        // Create the joints
        foreach (string entry in jointMap)
        {
            // do something with entry.Value or entry.Key
            // assuming it has a line renderer
            GameObject jointObj = new GameObject();

            jointObj.transform.localScale = parent.transform.localScale / 2;
            jointObj.name = entry;
            jointObj.transform.parent = parent.transform;

            // now for the joint struct
            if (joints[count] == null)
            {
                joints[count] = new KinectJoint();
                joints[count].name = jointObj.name;
                count++;
            }
        }

        // Create the boxes
        foreach (KeyValuePair<string, string> entry in boneMap) {
            GameObject jointParent = this.transform.FindChild(entry.Value).gameObject;
            GameObject joint = this.transform.FindChild(entry.Key).gameObject;

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
            box.name = entry.Value + "_" + entry.Key;
            box.transform.parent = parent.transform;
        }
    }

    void buildBodyStars(GameObject parent)
    {
        int count = 0;

        foreach (string entry in jointMap)
        {
            // do something with entry.Value or entry.Key
            // assuming it has a line renderer
            GameObject jointObj = new GameObject();

            GameObject particles = (GameObject)Instantiate(ParticleStarPrefab, jointObj.transform.position, Quaternion.identity);
            particles.transform.parent = jointObj.transform;

            jointObj.transform.localScale = parent.transform.localScale / 2;
            jointObj.name = entry;
            jointObj.transform.parent = parent.transform;

            // now for the joint struct
            if (joints[count] == null)
            {
                joints[count] = new KinectJoint();
                joints[count].name = jointObj.name;
                count++;
            }
        }

        // Extra particles
        foreach (KeyValuePair<string, string> entry in boneMap)
        {
            GameObject jointObj = new GameObject();
            jointObj.name = entry.Value + "_" + entry.Key;
            jointObj.transform.parent = parent.transform;

            GameObject particles = (GameObject)Instantiate(ParticleStarPrefab, Vector3.zero, Quaternion.identity);
            particles.transform.parent = jointObj.transform;
        }

    }

    private void sampleKinectData(string data, float timeRate, int frameRate)
    {
        string[] lines = data.Split('\n');
        float currentTime = 0;
        string[] str = new string[25];

        // last newline is always empty
        for (int i = 0; i < lines.Length - 1; i += frameRate)
        {
            string[] values = lines[i].Split(' ');

            // Kinect = 25 joints
            for (int joint = 0; joint < 25; ++joint)
            {
                // Kinect doesn't need rotations, so for that we use 0 0 0
                str[joint] += currentTime + " " + values[joint * 3 + 0] + " " + values[joint * 3 + 1] + " " + values[joint * 3 + 2] + " 0.0 0.0 0.0" + Environment.NewLine;
            }

            currentTime += timeRate;
        }

        for (int joint = 0; joint < 25; ++joint)
            joints[joint].addKeyFrameAnimation(str[joint].TrimEnd(Environment.NewLine.ToCharArray()));

        // lets save the totnal number of frames
        totalFrames = joints[0].ani.getNumberOfKeyFrames(); //will be the same for any joint 
    }

    void moveBody(KinectJoint[] joints, int frame)
    {
        Vector3 parentPosition = this.transform.position;
        Vector3 parentScale = this.transform.localScale;

        for (int i = 0; i < joints.Length; ++i) {
            this.transform.FindChild(joints[i].name).position = parentPosition + Vector3.Scale(parentScale, joints[i].ani.getPosition(frame) * scaleFactor);
        }
    }

    void moveBodyLinear(KinectJoint[] joints, int keyFrameCurrent, int keyFrameNext, float u)
    {
        Vector3 parentPosition = this.transform.position;
        Vector3 parentScale = this.transform.localScale;

        for (int i = 0; i < joints.Length; ++i)
        {
            this.transform.FindChild(joints[i].name).position = parentPosition + Vector3.Scale(parentScale, joints[i].ani.interpolationLinearPos(keyFrameCurrent, keyFrameNext, u) * scaleFactor);
        }
    }

    void refreshBodyLines()
    {
        foreach (KeyValuePair<string, string> entry in boneMap)
        {
            // assuming it has a line renderer
            GameObject jointParent = this.transform.FindChild(entry.Value).gameObject;
            GameObject joint = this.transform.FindChild(entry.Key).gameObject;

            LineRenderer lr = joint.GetComponent<LineRenderer>();
            lr.SetPosition(0, joint.transform.position);
            lr.SetPosition(1, jointParent.transform.position);
            lr.SetColors(Color.green, Color.green);
        }
    }

    void refreshBodyParticles() {
        foreach (KeyValuePair<string, string> entry in boneMap)
        {
            GameObject jointParent = this.transform.FindChild(entry.Value).gameObject;
            GameObject joint = this.transform.FindChild(entry.Key).gameObject;

            GameObject extraParticle = GameObject.Find(entry.Value + "_" + entry.Key);
            extraParticle.transform.position = middlePoint(jointParent.transform.position, joint.transform.position);
        }
    }

    void refreshBodyBlock()
    {
        foreach (KeyValuePair<string, string> entry in boneMap)
        {
            GameObject jointParent = this.transform.FindChild(entry.Value).gameObject;
            GameObject joint = this.transform.FindChild(entry.Key).gameObject;

            Vector3 between = jointParent.transform.position - joint.transform.position;
            float distance = between.magnitude;

            GameObject box = GameObject.Find(entry.Value + "_" + entry.Key);

            box.transform.localScale = 30 * new Vector3(distance / 4.0f, distance / 4.0f, distance);
            box.transform.position = joint.transform.position + (between / 2.0f);
            box.transform.LookAt(jointParent.transform.position, between);
        }
    }

    private Vector3 middlePoint(Vector3 v, Vector3 u) {
        return new Vector3((v.x + u.x)/2.0f,(v.y + u.y)/2.0f,(v.z + u.z)/2.0f);
    }
}
