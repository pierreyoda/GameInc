using System;
using UnityEngine;

public class WorldController : MonoBehaviour {
    [SerializeField] private World world;
    [SerializeField] private GameDevCompany playerCompany;
    [SerializeField] private EventsController eventsController;
    [SerializeField] private NewsController newsController;
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

    public void OnGameStarted(Database.Database database, DateTime gameDateTime, GameDevCompany playerCompany) {
        this.playerCompany = playerCompany;
        eventsController.InitEvents(database.Events.Collection, database.Texts.Collection);
        eventsController.InitVariables(gameDateTime, playerCompany);
        newsController.InitNews(database.News.Collection, gameDateTime);
    }

    public void OnDateModified(DateTime gameDateTime) {
        eventsController.OnGameDateChanged(gameDateTime, playerCompany);
        newsController.OnGameDateChanged(gameDateTime);
        hudController.UpdateDateDisplay(gameDateTime);
    }

    public void OnPlayerCompanyModified(DateTime gameDateTime) {
        eventsController.OnPlayerCompanyChanged(gameDateTime, playerCompany);
        hudController.UpdateCompanyHud(playerCompany);
    }
}