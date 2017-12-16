using UnityEngine;

public class WorldController : MonoBehaviour {
    public World world;

    public void ToggleSimulationPause() {
        world.ToggleSimulation();
    }

    public void ToggleSimulationSpeed() {
        world.ToggleSimulationSpeed();
    }

    public void BuildRoom(string roomId) {
        world.BuildNewRoom(roomId);
    }
}