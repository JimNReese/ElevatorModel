using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data;
//using System.Data.Entity;
using System.Web.Mvc;
using System.Collections;
using System.ComponentModel.DataAnnotations;


namespace ElevatorWebApp.Models
{
    public class CallButton
    {
        public enum CallButtonType
        {
            //Elevator Request Button Type Resides on a Floor
            [Display(Name = "Elevator Request Button")]
            ElevatorRequest, // This CallButtonType resides on a Floor and Requests an Elevator

            //Floor Request Button Type Resides in an Elevator labeled with a Floor
            [Display(Name = "Floor Request Button")]
            FloorRequest     // This CallButtonType resides inside an Elevator and Requests a Floor
        }

        [Required]
        [Display(Name = "Floor Number")]
        public Nullable<int> Floor { get; private set; }

        [Display(Name = "Last Press Time")]
        public DateTime LastPressDateTime { get; private set; }

        public CallButtonType ButtonType { get; private set; } // Denotes whether this buton requests floors or requests elevators
        public ICollection<CallButtonPress> CallButtonPressHistory;   

        public CallButton(CallButtonType buttonType, int floorNumber)
        {
            ButtonType = buttonType;
            Floor = floorNumber;
            CallButtonPressHistory = new HashSet<CallButtonPress>();
        }


        public CallButtonPress PressButton(int floor)
        {
            LastPressDateTime = DateTime.Now;
            CallButtonPress callButtonPress = new CallButtonPress( ButtonType, floor);
            this.CallButtonPressHistory.Add(callButtonPress);
            return callButtonPress;
        }
    }
}