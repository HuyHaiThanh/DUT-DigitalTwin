using UnityEngine;

namespace DUT.Core
{
    [SelectionBase]
    public class RoomObject : MonoBehaviour
    {
        [Header("Room Info")]
        public string roomId;
        public string buildingName;
        
        [TextArea(3, 5)]
        public string description;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(roomId) && name.StartsWith("Room_"))
            {
                roomId = name.Replace("Room_", "");
            }
        }
    }
}
