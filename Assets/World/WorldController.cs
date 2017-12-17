using System;
using UnityEngine;

public class WorldController : MonoBehaviour {
    [SerializeField] private World world;
    [SerializeField] private GameHudController hudController;

    public void ToggleSimulationPause() {
        world.ToggleSimulation();
    }

    public void ToggleSimulationSpeed() {
        world.ToggleSimulationSpeed();
    }

    public void BuildRoom(string roomId) {
        world.BuildNewRoom(roomId);
    }

    public void OnBuildingClicked() {
        world.OnBuildingClicked();
    }

    public void OnDateModified(DateTime gameDateTime) {
        hudController.UpdateDateDisplay(gameDateTime);
    }

    public void OnPlayerCompanyModified(GameDevCompany playerCompany) {
        hudController.UpdateCompanyHud(playerCompany);
    }
}