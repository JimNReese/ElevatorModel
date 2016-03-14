using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElevatorWebApp.Models
{
    public class CallButtonPress
    {        
        public System.DateTime PressTime { get; private set; }
        public CallButton.CallButtonType ButtonType { get; private set; }
        public int PressFloor { get; private set; }
        public Status RequestStatus { get; internal set;}

        public enum Status
        {
            Pending,   // Request Pending
            Complete,  // Request Completed
        }

        //public CallButtonPress( DateTime pressTime, int pressFloor)
        public CallButtonPress(CallButton.CallButtonType buttonType, int pressFloor)
        {
            PressTime = DateTime.Now;
            ButtonType = buttonType;
            PressFloor = pressFloor;
            RequestStatus = Status.Pending;
        }
    }
}