using System;
using System.Collections.Generic;
using Database;
using UnityEngine;
using Event = Database.Event;

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

    public void OnGameStarted(List<Event> events, List<News> news, DateTime gameDateTime, GameDevCompany playerCompany) {
        this.playerCompany = playerCompany;
        eventsController.InitEvents(events);
        eventsController.InitVariables(gameDateTime, playerCompany);
        newsController.InitNews(news, gameDateTime);
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