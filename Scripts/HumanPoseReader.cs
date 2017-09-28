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
    [ExecuteInEditMode]
    public class HumanPoseReader : MonoBehaviour, TimelineCallable.TimelineCallable
    {
        public string loadPath;

        public bool playOnAwake = false;
        public bool loadInEditor = false;

        private Avatar avatar;
        private HumanPoseHandler poseHandler;
        private HumanPose currentPose;

        private Dictionary<float, HumanPose> poseDict;
        private List<float> sortedKeys;

        private bool isPlaying = false;
        private bool pathChanged = false;
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
        }

        private void OnValidate()
        {
            if (loadPath != null)
            {
                pathChanged = true;
            }
        }

        private void Update()
        {
            if (pathChanged)
            {
                pathChanged = false;
                LoadPoseData();
            }
        }

        private void LateUpdate()
        {
            if (!isPlaying)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    StartPlaying();
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
                    float endFrameTime;
                    while (currentFrame < totalFrames)
                    {
                        endFrameTime = sortedKeys[currentFrame];
                        if (endFrameTime >= currentTime)
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

        private void StartPlaying()
        {
            isPlaying = true;
            currentFrame = 0;
            currentTime = 0f;
            Debug.Log("===STARTED PLAYING ANIMATION===");
            currentPose = poseDict[sortedKeys[currentFrame++]];
            poseHandler.SetHumanPose(ref currentPose);
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

        private void LoadPoseData()
        {
            if (!Application.isPlaying && !loadInEditor)
            {
                return;
            }
            if (loadPath == null)
            {
                Debug.LogError("***NO LOAD PATH SET ON " + gameObject.name + "***");
                return;
            }
            string[] splitPath = loadPath.Split('/');
            string filename = splitPath[splitPath.Length - 1];
            Debug.Log("===LOADING POSE: " + filename.ToUpper() + "===");
            
            BinaryFormatter formatter = new BinaryFormatter();
            SurrogateSelector surrogateSelector = new SurrogateSelector();
            Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();
            QuaternionSerializationSurrogate quatSS = new QuaternionSerializationSurrogate();

            surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
            surrogateSelector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), quatSS);
            formatter.SurrogateSelector = surrogateSelector;

            FileStream file = File.Open(loadPath, FileMode.Open);

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
            Debug.Log("===SUCCESSFULLY LOADED " + filename.ToUpper() + "===");
            if (playOnAwake)
            {
                StartPlaying();
            }
        }

        public void OnTimelineEvent()
        {
            StartPlaying();
        }
    }
}