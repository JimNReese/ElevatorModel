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
    public class ElevatorTeam
    {
        [Display(Name = "Elevator Team Name")]
        public string TeamName { get; set; }
        public int MaxFloors { get; set; }
        public int ElevatorCount { get; set; }

        // Collection Elevators in this ElevatorTeam
        public ICollection<Elevator> Elevators;

        // Collection of Buttons on each Floor which request Elevators
        public ICollection<CallButton> FloorButtonCollection;

        // Collection of Floor Button Presses which Requested Elevators
        public ICollection<CallButtonPress> ElevatorRequestCollection;

        public ElevatorTeam(string teamName, int maxFloors, int elevatorCount)
        {
            this.TeamName = teamName;
            this.MaxFloors = maxFloors;
            this.ElevatorCount = elevatorCount;
            // Add holder for collection of Elevators in this Elevator team
            this.Elevators = new HashSet<Elevator>();

            // Add Elevators to the ElevatorTeam
            for (int i = 1; i <= ElevatorCount; i++)
            {
                Elevator elevator = new Elevator(TeamName + " " + i.ToString(), MaxFloors);
                Elevators.Add(elevator);
            }

            // Add Floor buttons which request elevators to each floor of ElevatorTeam
            this.ElevatorRequestCollection = new HashSet<CallButtonPress>();
            for (int i = 1; i <= MaxFloors; i++)
            {
                CallButton floorButton = new CallButton(CallButton.CallButtonType.ElevatorRequest, i);
                FloorButtonCollection.Add(floorButton);
            }
            // Add holder for Floor Button presses which Requested Elevators
            this.ElevatorRequestCollection = new HashSet<CallButtonPress>();
        }

        // Press a button on this floor requesting an elevator
        public CallButtonPress PressElevatorCallButton(int elevatorRequestedToFloor)
        {
            // Of all the elevator request buttons on all the floors of this team of elevators 
            // Get the button on the floor that requested the elevator
            CallButton elevatorRequestedButton = FloorButtonCollection
                                                 .Where(e => e.Floor == elevatorRequestedToFloor).First();

            // Create a new elevator request as a result of this floors Elevator Request button press
            CallButtonPress elevatorRequest = elevatorRequestedButton.PressButton(elevatorRequestedToFloor);
            // Add the new elevator request to this elevator teams elevator request collection
            ElevatorRequestCollection.Add(elevatorRequest);
            ServiceElevatorRequest(elevatorRequest);
            return elevatorRequest;
        }

        public Int64 ReportTotalElevatorRequests()
        {
            Int64 totalElevatorRequests = 0;
            totalElevatorRequests = ElevatorRequestCollection.Count();
            return totalElevatorRequests;
        }

        public void ServiceElevatorRequest(CallButtonPress elevatorRequest)
        {
            // Get all the elevators in this ElevatorTeam not in Maintenance Mode 
            // and not with a Maintenance request pending
            ICollection<Elevator> availableElevators = Elevators
                .Where(e => (e.TravelingState != Elevator.TravelState.Maintenance) &&
                            (e.MaintenanceRequested == false))
                .Select(ae => ae).ToList();

            // Get first available elevator parked at this requested floor  
            Elevator availableElevatorParkedHere = availableElevators
                .Where(e => (e.TravelingState != Elevator.TravelState.Parked) &&
                            (e.CurrentFloor == elevatorRequest.PressFloor))
                .Select(aep => aep).First();
            // If an available elevator is parked at this requested floor then assign the elevator request to it
            if (availableElevatorParkedHere != null)
            {
                availableElevatorParkedHere.FloorRequestCollection.Add(elevatorRequest);

            }
            // Else Get all available elevators parked or travelling toward the floor requesting the elevator
            // of these get the nearest elevator 
            // (the one with the smallest difference in floors between requested floor and current floor)
            // OrderBy (ascending default) the Absolute value of the difference in floors 
            // between requested floor and current floor and pick the first one 
            else
            {
                Elevator nearestAvailableElevator = availableElevators
                    .Where(e => (e.TravelingState == Elevator.TravelState.Parked) ||
                    ((e.TravelingState == Elevator.TravelState.MovingUp) && (e.CurrentFloor < elevatorRequest.PressFloor)) ||
                    ((e.TravelingState == Elevator.TravelState.MovingDown) && (e.CurrentFloor > elevatorRequest.PressFloor)))
                    .OrderBy(dif => Math.Abs(dif.CurrentFloor - elevatorRequest.PressFloor))
                    .Select(nae => nae).First();
                // If an available elevator is parked near or approaching near this requested floor 
                // then assign the elevator request to it
                if (nearestAvailableElevator != null)
                {
                    nearestAvailableElevator.FloorRequestCollection.Add(elevatorRequest);
                }
                // Else Get any available elevators of these get the nearest elevator 
                // (the one with the smallest difference in floors between requested floor and current floor)
                // OrderBy (ascending default) the Absolute value of the difference in floors 
                // between requested floor and current floor and pick the first one
                else
                {
                    Elevator anyAvailableElevator = availableElevators                        
                        .OrderBy(dif => Math.Abs(dif.CurrentFloor - elevatorRequest.PressFloor))
                        .Select(nae => nae).First();
                    if (anyAvailableElevator != null)
                    {
                        anyAvailableElevator.FloorRequestCollection.Add(elevatorRequest);
                    }
                    else
                    {
                        // No elevators available Do Nothing 
                    }
                }
            }
        }
    }
}