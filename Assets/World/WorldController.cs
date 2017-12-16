using UnityEngine;

public class WorldController : MonoBehaviour {
    public World world;

    public void ToggleSimulationPause() {
        world.ToggleSimulation();
    }
}