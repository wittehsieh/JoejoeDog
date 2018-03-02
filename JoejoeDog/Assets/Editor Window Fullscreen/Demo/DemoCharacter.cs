using UnityEngine;
using System.Collections.Generic;

namespace EditorWindowFullscreenDemo
{
    public class DemoCharacter : MonoBehaviour
    {

        public List<GameObject> enteredRooms;
        public List<GameObject> gates;

        public static DemoCharacter instance;
        public static string[] hints; //Set by EditorDemoController
        public static int completedRooms = 0;
        private static bool debugMessages = false;

        float hintFadeoutTriggeredTime = 0;
        float hintFadeoutAfter = 0;
        float hintFadeoutTime = 0;
        private string hint = "";

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            enteredRooms = new List<GameObject>();
            completedRooms = 0;
            gates = new List<GameObject>();

            int i = 0;
            GameObject gate = null;
            while (i==0 || gate != null)
            {
                i++;
                gate = GameObject.Find("gate_" + i);
                if (gate != null) gates.Add(gate);
            }
            if (debugMessages) Debug.Log("Number of gates: " + gates.Count);
        }

        void Update()
        {

        }

        void FixedUpdate()
        {

        }

        void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.gameObject.name.Substring(0, 4) == "room")
            {
                if (!enteredRooms.Contains(collider.gameObject))
                    enteredRooms.Add(collider.gameObject);
                HintForRoom(enteredRooms.Count);
                if (debugMessages) Debug.Log("Entered room " + enteredRooms.Count);
            }
            else
            {
                if (debugMessages) Debug.Log("Collision with " + collider.gameObject.name);
            }
        }

        void HintForRoom(int roomNum)
        {
            if (roomNum <= completedRooms) return;
            
            if (hints != null)
            {
                hint = "Room Entered: " + roomNum;
                if (hints.Length >= roomNum)
                    hint = hints[roomNum - 1];
            }
            hintFadeoutAfter = 0;
        }

        public void FadeOutHint(float fadeOutAfterTime, float fadeoutTime)
        {
            hintFadeoutTriggeredTime = Time.realtimeSinceStartup;
            hintFadeoutAfter = fadeOutAfterTime;
            hintFadeoutTime = Mathf.Max(0.01f, fadeoutTime);
        }

        public void CompleteRoom(int roomNum)
        {
            CompleteRoom(roomNum, false);
        }
        public void CompleteRoom (int roomNum, bool hideGate)
        {
            if ((hideGate || enteredRooms.Count == roomNum) && roomNum <= gates.Count)
            {
                var gate = gates[roomNum - 1];
                if (gate != null)
                {
                    var rb = gate.GetComponent<Rigidbody2D>();
                    if (hideGate)
                    {
                        gate.SetActive(false);
                    }
                    else {
                        //Throw the gate out of the way.
                        rb.isKinematic = false;
                        var negative = Random.value > 0.5 ? 1 : -1;
                        rb.AddForce(new Vector2(Random.Range(0.5f, 1.2f) * negative, 0), ForceMode2D.Impulse);
                    }
                }
                FadeOutHint(3, 1);
                completedRooms = roomNum;
            }
        }

        void OnGUI()
        {
            var style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.cyan;
            style.alignment = TextAnchor.MiddleCenter;


            if (!string.IsNullOrEmpty(hint))
            {
                float opacity;
                if (hintFadeoutAfter <= 0) opacity = 1;
                else opacity = Mathf.Clamp01(hintFadeoutTriggeredTime + hintFadeoutAfter - Time.realtimeSinceStartup) / (hintFadeoutTime);
                if (opacity > 0)
                {
                    var hintPos = GUIUtility.ScreenToGUIPoint(new Vector2(Screen.width / 2, Screen.height / 3));
                    var color = GUI.color;
                    color.a = opacity;
                    GUI.color = color;
                    GUI.Label(new Rect(hintPos.x, hintPos.y, 0.2f, 0.2f), hint, style);
                    color.a = 1;
                    GUI.color = color;
                }
            }
        }
    }
}