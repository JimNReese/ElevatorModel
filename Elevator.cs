using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data;
using System.Web.Mvc;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace ElevatorWebApp.Models
{
    public class Elevator
    {
        //[Required]
        [Display(Name = "Elevator Id")]
        public Int64 Id { get;  }

        //[Required]
        [Display(Name = "Elevator Name")]
        public string ElevatorName { get; set; }
        public int MaxFloors { get; set; }
        public int CurrentFloor  { get; private set; }
        public int LastVisitedFloor { get; private set; }
        public int TargetFloor { get; private set; } // The next floor to visit
        public int FloorsTravelled { get; private set; } // Count of floors traversed
        public int FloorsVisited { get; private set; } // Count of floors stopped at

        // Collection of Elevator Buttons which Request Floors
        public ICollection<CallButton> ElevatorButtonCollection;

        // Collection of Elevator Button Presses which Requested Floors
        public ICollection<CallButtonPress> FloorRequestCollection;

        public TravelState TravelingState { get; private set; }
        public TravelState PreviousTravelingState { get; private set; }
        public bool MaintenanceRequested { get; private set; }

        public enum TravelState
        {
            [Display(Name = "Moving Up")]
            MovingUp,   // Elevator Moving Up
            [Display(Name = "Moving Down")]
            MovingDown, // Elevator Moving Down
            [Display(Name = "Parked at floor")]
            Parked,     // Elevator Parked at floor
            [Display(Name = "Maintenance Mode")]
            Maintenance // Elevator in Maintenance Mode
        }

        public Elevator(string name, int maxFloors)
        {
            this.ElevatorName = name;
            this.MaxFloors = maxFloors;
            // Add holder collection for Elevator Buttons labeled with floors on inside of Elevator
            this.ElevatorButtonCollection = new HashSet<CallButton>();
            // Add a button for every floor to the inside of the elevator
            for (int i = 1; i <= MaxFloors; i++)
            {
                CallButton newFloorButton = new CallButton(CallButton.CallButtonType.FloorRequest, i);
                this.ElevatorButtonCollection.Add(newFloorButton);
            }
            this.TravelingState = TravelState.Parked;
            this.PreviousTravelingState = TravelState.Parked;
            this.CurrentFloor = 1;
            this.TargetFloor = 1;
            this.MaintenanceRequested = false;

            // Add holder for collection of Elevator Button presses which Requested Floors
            this.FloorRequestCollection = new HashSet<CallButtonPress>();
        }

        public string ReportCurrentFloor()
        {
            return CurrentFloor.ToString();
        }

        public string ReportTravelingState()
        {
            return TravelingState.ToString();
        }

        public string ReportTargetFloor()
        {
            return TargetFloor.ToString();
        }

        public string ReportFloorsTravelled()
        {
            return FloorsTravelled.ToString();
        }

        public string ReportFloorsVisited()
        {
            return FloorsVisited.ToString();
        }

        public Int64 ReportServiceRequestsCompleted()
        {
            Int64 requestsCompleted = 0;
            requestsCompleted = FloorRequestCollection.Where(r => r.RequestStatus == CallButtonPress.Status.Complete)
                .Count();
            return requestsCompleted; 
        }

        public string ReportPendingFloorRequests()
        {
            string pendingFloorRequests = string.Empty;
            ICollection<CallButtonPress> requestedFloors = FloorRequestCollection
                .Where(r => r.RequestStatus == CallButtonPress.Status.Pending)
                .Select(f => f).ToList();
            foreach (CallButtonPress requestedFloor in requestedFloors)
            {
                pendingFloorRequests += requestedFloor.PressFloor.ToString() + "; ";
            }
            if (pendingFloorRequests == string.Empty)
            {
                pendingFloorRequests = "No pending floor requests.";
            }
            return pendingFloorRequests;
        }

        // Press a button inside this elevator requesting a floor
        public CallButtonPress PressFloor(int floorRequested)
        {
            // Of all the floor buttons inside this elevator get the button with the floor that was pressed
            CallButton floorRequestedButton = ElevatorButtonCollection
                                            .Where(e => e.Floor == floorRequested).First();
            // Create a new floor request as a result of this Floor button press
            CallButtonPress floorRequest = floorRequestedButton.PressButton(floorRequested);
            // Add the new floor request to this elevators floor request collection 
            // If not in Maintenace Mode and Maintenance Mode not requested
            if ((TravelingState != TravelState.Maintenance) && (MaintenanceRequested == false))
            {
                FloorRequestCollection.Add(floorRequest);
            }
            ServiceFloorRequests();
            return floorRequest;
        }

        // Find Next Request to service and Determine/Set Next Direction
        private CallButtonPress GetNextRequestSetDirection()
        {
            // NOTE Assuming request for Maintenance should complete pending 
            // requests before going into Maintenance; No new service request 
            // are being added once maintenance has been requested
            // Find Next Request to service and Determine Next Direction
            CallButtonPress nextRequest = null;
            if (PreviousTravelingState == TravelState.MovingDown)
            {
                // Get any requests for floors below current floor
                CallButtonPress floorsBelowRequest = FloorRequestCollection
                    .Where(r => (r.RequestStatus == CallButtonPress.Status.Pending) &&
                                (r.PressFloor < CurrentFloor))
                    .OrderByDescending(b => b.PressFloor)
                    .Select(fbr => fbr).First();
                if (floorsBelowRequest != null)
                {
                    nextRequest = floorsBelowRequest;
                    TargetFloor = floorsBelowRequest.PressFloor;
                    return nextRequest;
                }
            }
            else if (PreviousTravelingState == TravelState.MovingUp)
            {
                // Get any requests for floors above current floor
                CallButtonPress floorsAboveRequest = FloorRequestCollection
                    .Where(r => (r.RequestStatus == CallButtonPress.Status.Pending) &&
                                (r.PressFloor > CurrentFloor))
                    .OrderBy(b => b.PressFloor)
                    .Select(fbr => fbr).First();
                if (floorsAboveRequest != null)
                {
                    nextRequest = floorsAboveRequest;
                    TargetFloor = floorsAboveRequest.PressFloor;
                    return nextRequest;
                }
            }

            // At this point Next request not resolved, fall thru to here
            // So now (previousTravelingState == TravelState.Parked) OR 
            // No Floors requested in the previous direction of travel so get the first by DateTime Pending Request
            // As Long as current TravelingState != TravelState.Maintenance
            if (PreviousTravelingState != TravelState.Maintenance)
            {
                // Get any requests for floors anywhere OrderBy First Pressed
                CallButtonPress firstFloorRequest = FloorRequestCollection
                    .Where(r => (r.RequestStatus == CallButtonPress.Status.Pending))
                    .OrderBy(b => b.PressTime)
                    .Select(fr => fr).First();
                if (firstFloorRequest != null)
                {
                    nextRequest = firstFloorRequest;
                    TargetFloor = firstFloorRequest.PressFloor;
                    if (firstFloorRequest.PressFloor > CurrentFloor)
                    {
                        PreviousTravelingState = TravelingState;
                        TravelingState = TravelState.MovingUp;
                    }
                    else if (firstFloorRequest.PressFloor < CurrentFloor)
                    {
                        PreviousTravelingState = TravelingState;
                        TravelingState = TravelState.MovingUp;
                    }
                    return nextRequest;
                }
                else // There must be no pending floor requests so if Maintenanance Requested 
                    /// then enter Maintenance Mode otherwise Park Elevator Here
                {
                    PreviousTravelingState = TravelingState;
                    if (MaintenanceRequested == true)
                    {
                        TravelingState = TravelState.Maintenance;
                        TargetFloor = 1;
                        VisitFloor(1);
                    }
                    else
                    {
                        TravelingState = TravelState.Parked;
                        TargetFloor = CurrentFloor;
                    }
                }
            }
            // return null initilized request object since no pending request or in Maintenance
            return nextRequest; 
        }

        public void ServiceFloorRequests()
        {
            // Get the next request and direction
            CallButtonPress nextRequest = GetNextRequestSetDirection();
            if (nextRequest != null)
            {
                VisitFloor(nextRequest.PressFloor);
            }
        }

        private void OpenDoors(int currentFloor)
        {
            // OpenDoors
        }

        private void VisitFloor(int floor)
        {
            LastVisitedFloor = CurrentFloor;
            CurrentFloor = floor;
            TravelState previousTravelingState = TravelingState;
            if (TravelingState != TravelState.Maintenance)
            {
                TravelingState = TravelState.Parked;
            }
            // Open Elevator Doors
            OpenDoors(CurrentFloor);
            // Get all pending floor requests for the Current Floor
            ICollection<CallButtonPress> allPendingFloorRequestsCurrentFloor =
                FloorRequestCollection
                .Where(r => (r.RequestStatus == CallButtonPress.Status.Pending) &&
                            (r.PressFloor == CurrentFloor))
                .Select(pr => pr).ToList();
            // Mark as Completed all pending floor requests for the Current Floor since we're visiting it
            foreach (CallButtonPress pendingFloorRequestCurrentFloor in allPendingFloorRequestsCurrentFloor)
            {
                pendingFloorRequestCurrentFloor.RequestStatus = CallButtonPress.Status.Complete;
            }
            FloorsVisited++; // Increment FloorsVisited
            FloorsTravelled += Math.Abs(CurrentFloor - LastVisitedFloor); // Increment Travelled
            ServiceFloorRequests();
        }

        public bool RequestMaintenance()
        {
            MaintenanceRequested = true;
            return MaintenanceRequested;
        }

        public bool CancelMaintenance()
        {
            TravelingState = TravelState.Parked;
            MaintenanceRequested = false;
            return MaintenanceRequested;
        }
    }
}