using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Valve.VR;

namespace PoseRecorder
{
    public class HumanPoseReader : MonoBehaviour
    {
        [Header("File Location")]
        public string loadLocation;

        private Avatar avatar;
        private HumanPoseHandler poseHandler;
        private HumanPose currentPose;

        private Dictionary<float, HumanPose> poseDict;
        private List<float> sortedKeys;

        private bool isPlaying = false;
        private int currentFrame = 0;
        private int totalFrames = 0;

        private void OnEnable()
        {
            avatar = gameObject.GetComponent<Animator>().avatar;
            if (avatar == null)
            {
                Debug.LogError("No Avatar Found!");
                return;
            }
            poseHandler = new HumanPoseHandler(avatar, transform);
            loadLocation = Application.dataPath + "/Animations/TestAnimation.pose";
            LoadPoseData();
        }

        private void Update()
        {
            if (!isPlaying)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    isPlaying = true;
                    currentFrame = 0;
                    Debug.Log("===STARTED PLAYING ANIMATION===");
                }
            }
        }

        private void FixedUpdate()
        {
            if (isPlaying)
            {
                if (currentFrame >= totalFrames)
                {
                    isPlaying = false;
                    Debug.Log("===FINISHED PLAYING ANIMATION===");
                }
                else
                {
                    currentPose = poseDict[sortedKeys[currentFrame++]];
                    poseHandler.SetHumanPose(ref currentPose);
                }
            }
        }

        private void LoadPoseData()
        {
            Debug.Log("===LOADING POSE DATA===");
            
            BinaryFormatter formatter = new BinaryFormatter();
            SurrogateSelector surrogateSelector = new SurrogateSelector();
            Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();
            QuaternionSerializationSurrogate quatSS = new QuaternionSerializationSurrogate();

            surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
            surrogateSelector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), quatSS);
            formatter.SurrogateSelector = surrogateSelector;

            FileStream file = File.Open(loadLocation, FileMode.Open);

            Dictionary<float, RecordablePose> recordablePoseData = (Dictionary<float, RecordablePose>)formatter.Deserialize(file);
            file.Close();
            poseDict = new Dictionary<float, HumanPose>(recordablePoseData.Keys.Count);
            foreach (var key in recordablePoseData.Keys)
            {
                RecordablePose rPose = recordablePoseData[key];
                HumanPose pose = new HumanPose();
                pose.bodyPosition = rPose.bodyPosition;
                pose.bodyRotation = rPose.bodyRotation;
                pose.muscles = rPose.muscles;
                poseDict.Add(key, pose);
            }
            sortedKeys = new List<float>(recordablePoseData.Keys);
            sortedKeys.Sort();
            totalFrames = recordablePoseData.Keys.Count;
            Debug.Log("===LOADING COMPLETE===");
        }
    }
}