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
        private float currentTime = 0f;
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

        private void LateUpdate()
        {
            if (!isPlaying)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    isPlaying = true;
                    currentFrame = 0;
                    currentTime = 0f;
                    Debug.Log("===STARTED PLAYING ANIMATION===");
                    currentPose = poseDict[sortedKeys[currentFrame++]];
                    poseHandler.SetHumanPose(ref currentPose);
                }
            } else
            {
                if (currentFrame >= totalFrames - 1)
                {
                    isPlaying = false;
                    Debug.Log("===FINISHED PLAYING ANIMATION===");
                }
                else
                {
                    float oldTime = currentTime;
                    currentTime += Time.deltaTime;
                    float startFrameTime;
                    float endFrameTime;
                    while (currentFrame < totalFrames - 1)
                    {
                        startFrameTime = sortedKeys[currentFrame];
                        endFrameTime = sortedKeys[currentFrame + 1];
                        if (startFrameTime <= currentTime && endFrameTime >= currentTime)
                        {
                            float progessThroughFrame = (currentTime - oldTime) / (endFrameTime - oldTime);
                            if (progessThroughFrame > 1 || progessThroughFrame < 0)
                            {
                                Debug.LogError("***FRAME PROGRESS ERROR***");
                                Debug.LogError("Progress: " + progessThroughFrame + " Current Time: " + currentTime + " Current Frame: " + currentFrame);
                                return;
                            }
                            currentPose = getPoseLerp(progessThroughFrame, currentPose, poseDict[endFrameTime]);
                            poseHandler.SetHumanPose(ref currentPose);
                            return;
                        } else
                        {
                            currentFrame++;
                        }
                    }
                }
            }
        }

        private HumanPose getPoseLerp(float progress, HumanPose currentPose, HumanPose nextPose)
        {
            HumanPose poseToReturn = new HumanPose();
            poseToReturn.bodyPosition = Vector3.Lerp(currentPose.bodyPosition, nextPose.bodyPosition, progress);
            poseToReturn.bodyRotation = Quaternion.Lerp(currentPose.bodyRotation, nextPose.bodyRotation, progress);

            int minMuscles = currentPose.muscles.Length >= nextPose.muscles.Length ? currentPose.muscles.Length : nextPose.muscles.Length;
            float[] newMuscles = new float[minMuscles];
            for (int i = 0; i < minMuscles; i++)
            {
                newMuscles[i] = Mathf.Lerp(currentPose.muscles[i], nextPose.muscles[i], progress);
            }
            poseToReturn.muscles = newMuscles;
            return poseToReturn;
        }

        private void SetPose()
        {
            currentPose = poseDict[sortedKeys[currentFrame++]];
            poseHandler.SetHumanPose(ref currentPose);
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