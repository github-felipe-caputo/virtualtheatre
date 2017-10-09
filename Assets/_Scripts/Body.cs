using UnityEngine;
using System.Collections;
using System;

public class Body : MonoBehaviour {
    // the batch file with the key frame values
    public TextAsset BVHInput;

    // The particle system
    public GameObject particle;

    // animations happens between "current" and "next" keyframes
    private int keyFrameCurrent = 0;
    private int keyFrameNext = 1;

    private float timeFirstKeyFrame;
    private float timeLastKeyFrame;

    // Our bvh stuff
    private BVH bhv;
    BVH.Joint rt;
    BVH.Motion mt;

    // Use this for initialization
    void Start () {
        bhv = new BVH(BVHInput.text);
        
        rt = bhv.getRoot();
        print(rt.name);

        mt = bhv.getMotion();

        buildBody(rt);

        sampleBVH(rt, mt, 0.2f, 25);

        timeFirstKeyFrame = rt.ani.getTime(0);
        timeLastKeyFrame = rt.ani.getTime(rt.ani.getNumberOfKeyFrames() - 1);       
    }

    private string removeFirstOcurrenceSubstring(string s, string substring)
    {
        int index = s.IndexOf(substring);
        return (index < 0) ? s : s.Remove(index, substring.Length);
    }

    // Update is called once per frame
    void Update () {
        float time = Time.time;
        
        
        int i = Time.frameCount;
        if (i < mt.numFrames) {
            moveBody(rt, mt, i-1);
        }

        // update the line renders every frame too
        drawBody(rt);
        

        /*
        // Linear
        if (time >= timeFirstKeyFrame && time <= timeLastKeyFrame)
        {
            if (time < rt.ani.getTime(keyFrameNext))
            {
                float u = (time - rt.ani.getTime(keyFrameCurrent)) / (rt.ani.getTime(keyFrameNext) - rt.ani.getTime(keyFrameCurrent));
                moveBodyLinear(rt, mt, keyFrameCurrent, keyFrameNext, u);
            }
            else
            {
                ++keyFrameCurrent;
                ++keyFrameNext;
            }
        }
        */
        // casteljou
        /*
        if (time >= timeFirstKeyFrame && time <= timeLastKeyFrame)
        {
            float u = (time - timeFirstKeyFrame) / (timeLastKeyFrame - timeFirstKeyFrame);
            moveBodyCasteljau(rt, mt, u);
        }
        */
    }

    void moveBodyCasteljau(BVH.Joint jt, BVH.Motion mt, float u) {

        if (jt.name.Equals("EndSite"))
        {
            drawLine(GameObject.Find(jt.parent.name + "/" + jt.name));
        }
        else
        {
            GameObject currentJoint = GameObject.Find(jt.name);

            if (jt.name.Equals("Hips"))
            {
                currentJoint.transform.position = jt.ani.interpolateCasteljauPos(u);
                currentJoint.transform.rotation = jt.ani.interpolateCasteljauRot(u);
            }
            else
            {
                currentJoint.transform.rotation = (currentJoint.transform.parent.transform.rotation) * jt.ani.interpolateCasteljauRot(u);
                drawLine(currentJoint);
            }

            for (int i = 0; i < jt.children.Length; i++) {
                moveBodyCasteljau(jt.children[i], mt, u);
            }
        }
    }

    void moveBodyLinear(BVH.Joint jt, BVH.Motion mt, int keyFrameCurrent, int keyFrameNext, float u)
    {
        if (!jt.name.Equals("EndSite"))
        {
            GameObject currentJoint = GameObject.Find(jt.name);

            if (jt.name.Equals("Hips"))
            {
                currentJoint.transform.localScale = (this.transform.localScale);
                currentJoint.transform.position = (this.transform.rotation) * (this.transform.position + jt.ani.interpolationLinearPos(keyFrameCurrent, keyFrameNext, u));
                currentJoint.transform.rotation = (this.transform.rotation) * jt.ani.interpolationLinearRot(keyFrameCurrent, keyFrameNext, u);
            }
            else
            {
                currentJoint.transform.rotation = (currentJoint.transform.parent.transform.rotation) * jt.ani.interpolationLinearRot(keyFrameCurrent, keyFrameNext, u);
            }

            for (int i = 0; i < jt.children.Length; i++)
            {
                moveBodyLinear(jt.children[i], mt, keyFrameCurrent, keyFrameNext, u);
            }
        }
    }

    void moveBody(BVH.Joint jt, BVH.Motion mt, int frame)
    {
        int index = jt.channelOffset;
        int frameXtotalChanel = frame * mt.numTotalChannels;

        if (!jt.name.Equals("EndSite"))
        {
            GameObject currentJoint = GameObject.Find(jt.name);

            if (jt.name.Equals("Hips"))
            {
                currentJoint.transform.localScale = (this.transform.localScale);

                /*currentJoint.transform.position = (this.transform.rotation) * (this.transform.position +
                                                  new Vector3(mt.motionData[frameXtotalChanel + 0],
                                                              mt.motionData[frameXtotalChanel + 1],
                                                              mt.motionData[frameXtotalChanel + 2]));*/

                currentJoint.transform.position = (this.transform.rotation) * 
                    (this.transform.position + Vector3.Scale((new Vector3(mt.motionData[frameXtotalChanel + 0],
                                                              mt.motionData[frameXtotalChanel + 1],
                                                              mt.motionData[frameXtotalChanel + 2])), (this.transform.localScale)));

                currentJoint.transform.rotation = (this.transform.rotation)
                    * Quaternion.AngleAxis(mt.motionData[frameXtotalChanel + 3], new Vector3(0, 0, 1))
                    * Quaternion.AngleAxis(mt.motionData[frameXtotalChanel + 4], new Vector3(1, 0, 0))
                    * Quaternion.AngleAxis(mt.motionData[frameXtotalChanel + 5], new Vector3(0, 1, 0));
            }
            else
            {
                currentJoint.transform.rotation = (currentJoint.transform.parent.transform.rotation)
                    * Quaternion.AngleAxis(mt.motionData[frameXtotalChanel + (index)], new Vector3(0, 0, 1))
                    * Quaternion.AngleAxis(mt.motionData[frameXtotalChanel + (index + 1)], new Vector3(1, 0, 0))
                    * Quaternion.AngleAxis(mt.motionData[frameXtotalChanel + (index + 2)], new Vector3(0, 1, 0));
            }

            for (int i = 0; i < jt.children.Length; i++)
            {
                moveBody(jt.children[i], mt, frame);
            }
        }
    }

    void buildBody(BVH.Joint jt) {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = jt.name;

        //GameObject particles = Instantiate(particle, Vector3.zero, Quaternion.identity) as GameObject;
        //particles.transform.parent = sphere.transform;

        //Debug.Log(" Name: " + jt.name + " channeloffset: " + jt.channelOffset);

        if (jt.parent != null)
        {
            sphere.transform.localScale = (this.transform.localScale);
            sphere.transform.parent = GameObject.Find(jt.parent.name).transform;
            sphere.transform.position = GameObject.Find(jt.parent.name).transform.position + jt.offset;
            sphere.AddComponent<LineRenderer>();
        }
        else
        {
            sphere.transform.localScale = (this.transform.localScale);
            sphere.transform.parent = this.transform;
            sphere.transform.position = jt.offset;
        }
        
        if (!jt.name.Equals("EndSite"))
        {
            for (int i = 0; i < jt.children.Length; i++)
                buildBody(jt.children[i]);
        }
        
    }

    void drawLine(GameObject obj) {

        LineRenderer lr = obj.GetComponent<LineRenderer>();
        lr.SetVertexCount(2);
        lr.SetWidth(0.005f, 0.005f);
        lr.SetPosition(0, obj.transform.position);
        lr.SetPosition(1, obj.transform.parent.position);
    }

    void drawBody(BVH.Joint jt) {
        if (jt.name.Equals("EndSite"))
        {
            drawLine(GameObject.Find(jt.parent.name + "/" + jt.name));
        }
        else
        {
            GameObject currentJoint = GameObject.Find(jt.name);

            if (!jt.name.Equals("Hips")) {
                drawLine(currentJoint);
            }

            for (int i = 0; i < jt.children.Length; i++)
            {
                drawBody(jt.children[i]);
            }
        }
    }

    void sampleBVH(BVH.Joint jt, BVH.Motion mt, float timeRate, int frameRate) {
        //int currentFrame = 0;
        float currentTime = 0;
        int index = jt.channelOffset;
        int frameXtotalChanel;
        int totalChannels = mt.numTotalChannels;

        string str = "";

        // hips
        if (jt.name.Equals("Hips"))
        {
            for (int currentFrame = 0; currentFrame < mt.numFrames; currentFrame += frameRate)
            {
                frameXtotalChanel = currentFrame * totalChannels + jt.channelOffset;

                str += currentTime + " " + mt.motionData[frameXtotalChanel + 0] + " " + mt.motionData[frameXtotalChanel + 1] + " " + mt.motionData[frameXtotalChanel + 2]
                        + " " + mt.motionData[frameXtotalChanel + 4] + " " + mt.motionData[frameXtotalChanel + 5] + " " + mt.motionData[frameXtotalChanel + 3] + Environment.NewLine;

                currentTime += timeRate;
            }

            jt.addKeyFrameAnimation(str.TrimEnd(Environment.NewLine.ToCharArray()));
        }
        else if (!jt.name.Equals("EndSite"))
        {
            for (int currentFrame = 0; currentFrame < mt.numFrames; currentFrame += frameRate)
            {
                frameXtotalChanel = currentFrame * totalChannels + jt.channelOffset;

                str += currentTime + " " + jt.offset.x + " " + jt.offset.y + " " + jt.offset.z
                        + " " + mt.motionData[frameXtotalChanel + 1] + " " + mt.motionData[frameXtotalChanel + 2] + " " + mt.motionData[frameXtotalChanel + 0] + Environment.NewLine;

                currentTime += timeRate;
            }

            jt.addKeyFrameAnimation(str.TrimEnd(Environment.NewLine.ToCharArray()));
        }


        if (!jt.name.Equals("EndSite"))
        {
            for (int i = 0; i < jt.children.Length; i++)
                sampleBVH(jt.children[i], mt, timeRate, frameRate);
        }

    }
}
