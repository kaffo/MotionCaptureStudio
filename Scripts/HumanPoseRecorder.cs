﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Valve.VR;

namespace PoseRecorder {
    [Serializable]
    public struct RecordablePose
    {
        public Vector3 bodyPosition { get; set; }
        public Quaternion bodyRotation { get; set; }
        public float[] muscles { get; set; }

    }

    public class Vector3SerializationSurrogate : ISerializationSurrogate
    {

        // Method called to serialize a Vector3 object
        public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context)
        {

            Vector3 v3 = (Vector3)obj;
            info.AddValue("x", v3.x);
            info.AddValue("y", v3.y);
            info.AddValue("z", v3.z);
        }

        // Method called to deserialize a Vector3 object
        public System.Object SetObjectData(System.Object obj, SerializationInfo info,
                                           StreamingContext context, ISurrogateSelector selector)
        {

            Vector3 v3 = (Vector3)obj;
            v3.x = (float)info.GetValue("x", typeof(float));
            v3.y = (float)info.GetValue("y", typeof(float));
            v3.z = (float)info.GetValue("z", typeof(float));
            obj = v3;
            return obj;
        }
    }

    public class QuaternionSerializationSurrogate : ISerializationSurrogate
    {

        // Method called to serialize a Quaternion object
        public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context)
        {

            Quaternion q = (Quaternion)obj;
            info.AddValue("x", q.x);
            info.AddValue("y", q.y);
            info.AddValue("z", q.z);
            info.AddValue("w", q.w);
        }

        // Method called to deserialize a Quaternion object
        public System.Object SetObjectData(System.Object obj, SerializationInfo info,
                                           StreamingContext context, ISurrogateSelector selector)
        {

            Quaternion q = (Quaternion)obj;
            q.x = (float)info.GetValue("x", typeof(float));
            q.y = (float)info.GetValue("y", typeof(float));
            q.z = (float)info.GetValue("z", typeof(float));
            q.w = (float)info.GetValue("w", typeof(float));
            obj = q;
            return obj;
        }
    }
    [ExecuteInEditMode]
    public class HumanPoseRecorder : MonoBehaviour, TimelineCallable.TimelineCallable
    {
        public string savePath;
        public string fileName;

        public SteamVR_TrackedObject leftHand;
        public SteamVR_TrackedObject rightHand;

        public Valve.VR.EVRButtonId startRecordKey = EVRButtonId.k_EButton_Grip;
        public Valve.VR.EVRButtonId stopRecordKey = EVRButtonId.k_EButton_SteamVR_Touchpad;

        public bool recordLimitedFrames = false;
        public int frameNo = 1000;

        private Avatar avatar;
        private HumanPoseHandler poseHandler;
        private HumanPose poseData;
        private RecordablePose recordablePose;

        private Dictionary<float, RecordablePose> poseDict;
        private float currentTime;
        private int currentFrame;

        private bool recording = false;

        public SteamVR_Controller.Device leftDevice
        {
            get
            {
                return SteamVR_Controller.Input((int)leftHand.index);
            }
        }

        public SteamVR_Controller.Device rightDevice
        {
            get
            {
                return SteamVR_Controller.Input((int)rightHand.index);
            }
        }

        private void OnEnable()
        {
            avatar = gameObject.GetComponent<Animator>().avatar;
            if (avatar == null)
            {
                Debug.LogError("No Avatar Found!");
                return;
            }
            poseHandler = new HumanPoseHandler(avatar, transform);
            poseDict = new Dictionary<float, RecordablePose>(frameNo);
        }

        private void LateUpdate()
        {
            if (recording)
            {
                if (rightDevice.GetPress(stopRecordKey))
                {
                    StopRecording();
                } else
                {
                    currentTime += Time.deltaTime;
                    UpdatePose();
                    if (recordLimitedFrames && currentFrame >= frameNo)
                    {
                        StopRecording();
                    }
                }

            } else
            {
                if (rightDevice.GetPress(startRecordKey))
                {
                    StartRecording();
                }
            }
        }

        public void StartRecording()
        {
            if (fileName == null || savePath == null)
            {
                Debug.LogError("***NO PATH SET***");
                return;
            }
            Debug.Log("===START RECORDING===");
            currentTime = 0f;
            currentFrame = 0;
            recording = true;
            UpdatePose();
        }

        public void StopRecording()
        {
            recording = false;
            Debug.Log("===END RECORDING===");
            SavePoseData();
        }

        private void UpdatePose()
        {
            poseHandler.GetHumanPose(ref poseData);
            recordablePose = new RecordablePose();
            recordablePose.bodyPosition = poseData.bodyPosition;
            recordablePose.bodyRotation = poseData.bodyRotation;
            recordablePose.muscles = (float[])poseData.muscles.Clone();

            poseDict.Add(currentTime, recordablePose);
            currentFrame++;
        }

        private void SavePoseData()
        {
            Debug.Log("===SAVING POSE DATA===");
            
            BinaryFormatter formatter = new BinaryFormatter();
            SurrogateSelector surrogateSelector = new SurrogateSelector();
            Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();
            QuaternionSerializationSurrogate quatSS = new QuaternionSerializationSurrogate();

            surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
            surrogateSelector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), quatSS);
            formatter.SurrogateSelector = surrogateSelector;

            FileStream file = File.Create(savePath + fileName);
            formatter.Serialize(file, poseDict);
            file.Close();
        }

        public void OnTimelineEvent()
        {
            StartRecording();
        }
    }
}
