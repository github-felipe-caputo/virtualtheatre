using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class KinectBody : MonoBehaviour {

    // bone material, necessary for colors
    public Material BoneMaterial;

    // Line renderer thickness
    public float LineThickness;

    // scaling the body
    // this scalesthe distance between the joints, it's encessary even when taking the parent's,
    // scaling into account
    public float scaleFactor;

    // UDP variables
    private int listenPort = 15000;
    private UdpClient reciever;
    private IPEndPoint endIP;
    private Thread rthread;
    float[,] joints = new float[25, 3];

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

    private bool connectFlag = false;

    // Block man or stick figure?
    public bool StickMan;
    public bool BlockMan;
    public bool ParticleMan;
    public bool StarMan;

    public GameObject ParticleStarPrefab;
    public GameObject ParticleSystemPrefab;
    public GameObject CollisionPlane;

    // Use this for initialization
    void Start () {
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
    }
	
	// Update is called once per frame
	void Update () {
        Vector3 parentPosition = this.transform.position;
        Vector3 parentScale = this.transform.localScale;

        // Update all of the children (joints) positions
        this.transform.FindChild("SpineBase").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[0, 0], joints[0, 1],  joints[0, 2]) * scaleFactor);
        this.transform.FindChild("SpineMid").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[1, 0], joints[1, 1],  joints[1, 2]) * scaleFactor);
        this.transform.FindChild("Neck").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[2, 0], joints[2, 1],  joints[2, 2]) * scaleFactor);
        this.transform.FindChild("Head").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[3, 0], joints[3, 1],  joints[3, 2]) * scaleFactor);
        this.transform.FindChild("ShoulderLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[4, 0], joints[4, 1],  joints[4, 2]) * scaleFactor);
        this.transform.FindChild("ElbowLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[5, 0], joints[5, 1],  joints[5, 2]) * scaleFactor);
        this.transform.FindChild("WristLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[6, 0], joints[6, 1],  joints[6, 2]) * scaleFactor);
        this.transform.FindChild("HandLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[7, 0], joints[7, 1],  joints[7, 2]) * scaleFactor);
        this.transform.FindChild("ShoulderRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[8, 0], joints[8, 1],  joints[8, 2]) * scaleFactor);
        this.transform.FindChild("ElbowRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[9, 0], joints[9, 1],  joints[9, 2]) * scaleFactor);
        this.transform.FindChild("WristRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[10, 0], joints[10, 1],  joints[10, 2]) * scaleFactor);
        this.transform.FindChild("HandRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[11, 0], joints[11, 1],  joints[11, 2]) * scaleFactor);
        this.transform.FindChild("HipLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[12, 0], joints[12, 1],  joints[12, 2]) * scaleFactor);
        this.transform.FindChild("KneeLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[13, 0], joints[13, 1],  joints[13, 2]) * scaleFactor);
        this.transform.FindChild("AnkleLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[14, 0], joints[14, 1],  joints[14, 2]) * scaleFactor);
        this.transform.FindChild("FootLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[15, 0], joints[15, 1],  joints[15, 2]) * scaleFactor);
        this.transform.FindChild("HipRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[16, 0], joints[16, 1],  joints[16, 2]) * scaleFactor);
        this.transform.FindChild("KneeRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[17, 0], joints[17, 1],  joints[17, 2]) * scaleFactor);
        this.transform.FindChild("AnkleRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[18, 0], joints[18, 1],  joints[18, 2]) * scaleFactor);
        this.transform.FindChild("FootRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[19, 0], joints[19, 1],  joints[19, 2]) * scaleFactor);
        this.transform.FindChild("SpineShoulder").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[20, 0], joints[20, 1],  joints[20, 2]) * scaleFactor);
        this.transform.FindChild("HandTipLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[21, 0], joints[21, 1],  joints[21, 2]) * scaleFactor);
        this.transform.FindChild("ThumbLeft").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[22, 0], joints[22, 1],  joints[22, 2]) * scaleFactor);
        this.transform.FindChild("HandTipRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[23, 0], joints[23, 1],  joints[23, 2]) * scaleFactor);
        this.transform.FindChild("ThumbRight").position = parentPosition + Vector3.Scale(parentScale, new Vector3(joints[24, 0], joints[24, 1],  joints[24, 2]) * scaleFactor);

        if (BlockMan)
        {
            refreshBodyBlock();
        }
        else if (ParticleMan || StarMan)
        {
            refreshBodyParticles();
        }
        else if (StickMan)
        {
            refreshBodyLines();
        }

    }

    void buildBody(GameObject parent)
    {
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
        }
    }

    void buildBodyParticles(GameObject parent)
    {
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
        // Create the joints
        foreach (string entry in jointMap)
        {
            // do something with entry.Value or entry.Key
            // assuming it has a line renderer
            GameObject jointObj = new GameObject();

            jointObj.transform.localScale = parent.transform.localScale / 2;
            jointObj.name = entry;
            jointObj.transform.parent = parent.transform;
        }

        // Create the boxes
        foreach (KeyValuePair<string, string> entry in boneMap)
        {
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

    void refreshBodyParticles()
    {
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


    private Vector3 middlePoint(Vector3 v, Vector3 u)
    {
        return new Vector3((v.x + u.x) / 2.0f, (v.y + u.y) / 2.0f, (v.z + u.z) / 2.0f);
    }


    void getData()
    {
        try
        {
            while (true)
            {
                byte[] buffer = reciever.Receive(ref endIP);
                float[] joints_flat = new float[buffer.Length / sizeof(float)];
                Buffer.BlockCopy(buffer, 0, joints_flat, 0, buffer.Length);

                // resize
                for (int i = 0; i < 25; i++)
                    for (int j = 0; j < 3; j++)
                        joints[i, j] = joints_flat[i * 3 + j];
            }
        }
        catch (Exception e)
        {
            print("Error" + e);
        }
    }

    void OnApplicationQuit()
    {
        if (rthread != null && rthread.IsAlive)
        {
            print("Killing...");
            rthread.Abort();
        }
        if (reciever != null)
        {
            reciever.Close();
        }
    }

    public void OnConnectClicked()
    {
        if (connectFlag == false)
        {
            try
            {
                reciever = new UdpClient(listenPort); // for a receiver, needs the port when creating udpclient
                endIP = new IPEndPoint(IPAddress.Any, listenPort);
                rthread = new Thread(new ThreadStart(getData));
                rthread.Start();
            }
            catch (Exception e)
            {
                print("Error" + e);
            }

            GameObject.Find("ConnectText").GetComponent<Text>().text = "Disconnect";
            GameObject.Find("FoundTargetText").GetComponent<Text>().text = "";
            connectFlag = true;
        }
        else {
            if (rthread != null && rthread.IsAlive)
            {
                print("Killing...");
                rthread.Abort();
            }
            if (reciever != null)
            {
                reciever.Close();
            }

            GameObject.Find("ConnectText").GetComponent<Text>().text = "Connect";
            connectFlag = false;

            // Reset text
            GameObject.Find("FoundTargetText").GetComponent<Text>().text = "Found" + '\n' + "Target";

            // Destroy current body
            int childs = this.transform.childCount;
            for (int i = 0; i < childs; ++i)
                GameObject.Destroy(transform.GetChild(i).gameObject);

            // Set stickman as default starting animation
            buildBody(this.gameObject);
            StickMan = true;
            BlockMan = false;
            ParticleMan = false;
        }
    }

    public void OnStickClicked()
    {
        int childs = this.transform.childCount;

        if (StickMan == false)
        {
            for (int i = 0; i < childs; ++i)
                GameObject.Destroy(transform.GetChild(i).gameObject);
            BlockMan = false;
            ParticleMan = false;
            StarMan = false;

            buildBody(this.gameObject);
            StickMan = true;
        }
    }

    public void OnBlockClicked()
    {
        int childs = this.transform.childCount;


        if (BlockMan == false)
        {
            for (int i = 0; i < childs; ++i)
                GameObject.Destroy(transform.GetChild(i).gameObject);
            StickMan = false;
            ParticleMan = false;
            StarMan = false;

            buildBodyBox(this.gameObject);
            BlockMan = true;
        }
    }

    public void OnParticleClicked()
    {
        int childs = this.transform.childCount;

        if (ParticleMan == false)
        {
            for (int i = 0; i < childs; ++i)
                GameObject.Destroy(transform.GetChild(i).gameObject);
            StickMan = false;
            BlockMan = false;
            StarMan = false;

            buildBodyParticles(this.gameObject);
            ParticleMan = true;
        }
    }

    public void OnStarClicked()
    {
        int childs = this.transform.childCount;

        if (StarMan == false)
        {
            for (int i = 0; i < childs; ++i)
                GameObject.Destroy(transform.GetChild(i).gameObject);
            StickMan = false;
            BlockMan = false;
            ParticleMan = false;

            buildBodyStars(this.gameObject);
            StarMan = true;
        }
    }
}
