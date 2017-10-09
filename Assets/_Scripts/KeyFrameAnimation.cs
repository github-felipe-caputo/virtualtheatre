using UnityEngine;
using System.Collections;

/* This class will be used to add animation to an object, based on keyframing */
public class KeyFrameAnimation {

    // time[i] = time of keyframe[i]
    private float[] time;

    // position[i] = position of object on keyframe[i]
    private Vector3[] position;

    // rotation[i] = rotation of object on keyframe[i]
    private Quaternion[] rotation;

    // total number of keyframes
    private int numberOfFrames;

    /*************** CONSTRUCTORS ***************/

    // Constructor
    public KeyFrameAnimation(string file) {
        // here we will parse the file using newlines
        string[] lines = file.Split('\n'); 

        // Initialize our arrays and values
        numberOfFrames = lines.Length;
        time = new float[numberOfFrames];
        position = new Vector3[numberOfFrames];
        rotation = new Quaternion[numberOfFrames];

        // Lets split the values vecause we know evey line is t (x,y,z) anglex angley anglez
        for (int i = 0; i < lines.Length; ++i)
        {
            string[] keyFrameParts = lines[i].Split(' ');
            time[i] = float.Parse(keyFrameParts[0]);
            position[i] = new Vector3(float.Parse(keyFrameParts[1]), float.Parse(keyFrameParts[2]), float.Parse(keyFrameParts[3]));
            rotation[i] = Quaternion.AngleAxis(float.Parse(keyFrameParts[6]), new Vector3(0, 0, 1))
                    * Quaternion.AngleAxis(float.Parse(keyFrameParts[4]), new Vector3(1, 0, 0))
                    * Quaternion.AngleAxis(float.Parse(keyFrameParts[5]), new Vector3(0, 1, 0));
        }
    }

    /*************** PRIVATE FUNCTIONS ***************/

    // Returns a normalized quaternion
    private Quaternion normalize(Quaternion q)
    {
        float den = Mathf.Sqrt(Mathf.Pow(q.x, 2) + Mathf.Pow(q.y, 2) + Mathf.Pow(q.z, 2) + Mathf.Pow(q.w, 2));
        return new Quaternion(q.x / den, q.y / den, q.z / den, q.w / den);
    }

    // Interpolates position using deCasteljau Construction method at at "time" u (0 <= u <= 1) 
    private Vector3 casteljauPosRecusion(Vector3[] p, float u)
    {
        if (p.Length == 2)
        {
            return Vector3.Lerp(p[0], p[1], u);
        }
        else
        {
            Vector3[] pnew = new Vector3[p.Length - 1];
            for (int i = 0; i < p.Length - 1; ++i)
                pnew[i] = Vector3.Lerp(p[i], p[i + 1], u);
            return casteljauPosRecusion(pnew, u);
        }
    }

    // Interpolates rotation using deCasteljau Construction method at at "time" u (0 <= u <= 1) 
    private Quaternion casteljauQuaRecusion(Quaternion[] q, float u)
    {
        if (q.Length == 2)
        {
            return normalize(Quaternion.Slerp(q[0], q[1], u));
        }
        else
        {
            Quaternion[] qnew = new Quaternion[q.Length - 1];
            for (int i = 0; i < q.Length - 1; ++i)
                qnew[i] = normalize(Quaternion.Slerp(q[i], q[i + 1], u));
            return casteljauQuaRecusion(qnew, u);
        }
    }

    /*************** PUBLIC FUNCTIONS ***************/

    // Returns time of keyframe i
    public int getNumberOfKeyFrames()
    {
        return numberOfFrames;
    }

    // Returns time of keyframe i
    public float getTime(int i)
    {
        return time[i];
    }

    // Returns position of keyframe i
    public Vector3 getPosition(int i)
    {
        return position[i];
    }

    // Returns rotation of keyframe i
    public Quaternion getRotation(int i)
    {
        return rotation[i];
    }

    // Return a linear interpolation of the positions of frames i and j, at "time" u (0 <= u <= 1)
    public Vector3 interpolationLinearPos(int i, int j, float u)
    {
        return Vector3.Lerp(position[i], position[j], u);
    }

    // Returns a linear interpolation of the rotations of frames i and j, at "time" u (0 <= u <= 1)
    public Quaternion interpolationLinearRot(int i, int j, float u)
    {
        return normalize(Quaternion.Slerp(rotation[i], rotation[j], u));
    }

    // Returns a linear interpolation of the positions using the TCB method, the interpolation
    // result is between points p1 and p2, using values t, c and b, and at "time" u (0 <= u <= 1)
    // OBS: Catmull Rom interpolation = T = C = B = 0
    public Vector3 interpolateTCBPos(int p0, int p1, int p2, int p3, float t, float c, float b, float u)
    {
        Vector3 DSiplus1 = ((1 - t) * (1 - c) * (1 + b) / 2 * (position[p2] - position[p1])) + ((1 - t) * (1 + c) * (1 - b) / 2 * (position[p3] - position[p2]));
        Vector3 DDi = ((1 - t) * (1 + c) * (1 + b) / 2 * (position[p1] - position[p0])) + ((1 - t) * (1 - c) * (1 - b) / 2 * (position[p2] - position[p1]));

        return Mathf.Pow(u, 3) * (2 * position[p1] - 2 * position[p2] + DDi + DSiplus1) + Mathf.Pow(u, 2) * (-3 * position[p1] + 3 * position[p2] - 2 * DDi - DSiplus1) + u * DDi + position[p1];
    }

    // Returns the result of interpolating all the position points at "time" u (0 <= u <= 1),
    // since this functions works recursevily using the list of points, we call another function
    // that will do that work
    public Vector3 interpolateCasteljauPos(float u) {
        return casteljauPosRecusion(position, u);
    }

    // Returns the result of interpolating all the rotations points at "time" u (0 <= u <= 1),
    // since this functions works recursevily using the list of points, we call another function
    // that will do that work
    public Quaternion interpolateCasteljauRot(float u) {
        return casteljauQuaRecusion(rotation, u);
    }
}
