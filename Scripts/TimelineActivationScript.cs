using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TimelineCallable {
    public interface TimelineCallable
    {
        void OnTimelineEvent();
    }

    public class TimelineActivationScript : MonoBehaviour {
        public TimelineCallable[] toEnable;

        private void OnEnable()
        {
            foreach (var item in toEnable)
            {
                item.OnTimelineEvent();
            }
            gameObject.SetActive(false);
        }
    }
}
