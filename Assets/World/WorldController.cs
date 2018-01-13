using System;
using System.Collections.Generic;
using Database;
using UnityEngine;
using Event = Database.Event;

public class WorldController : MonoBehaviour {
    private Database.Database database;
    private DateTime gameDateTime;
    [SerializeField] private World world;
    [SerializeField] private GameDevCompany playerCompany;
    [SerializeField] private EngineFeaturesController engineFeaturesController;
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

    public void StartProject() {
        hudController.ShowNewProjectDialog(gameDateTime,
            Project.ProjectType.GameProject, database,
            playerCompany.GameEngines);
    }

    public void OnGameStarted(Database.Database database, DateTime gameDateTime,
        GameDevCompany playerCompany) {
        this.database = database;
        this.playerCompany = playerCompany;
        this.gameDateTime = gameDateTime;
        engineFeaturesController.InitFeatures(database.EngineFeatures.Collection);
        engineFeaturesController.CheckFeatures(eventsController, gameDateTime, playerCompany);
        eventsController.InitEvents(database.Events.Collection);
        eventsController.InitVariables(gameDateTime, playerCompany);
        newsController.InitNews(database.News.Collection, gameDateTime);

        float hiringCost;
        Employee employee = playerCompany.EmployeesManager.GenerateRandomEmployee(
            eventsController, gameDateTime, playerCompany,
            database.HiringMethod.FindById("CompSciGraduates"),
            database.Names.FindById("CommonNamesUSA"),
            database.Skills,
            out hiringCost);
        playerCompany.AddEmployee(employee);
        Debug.Log($"Generated Random Employee : hiring cost = {hiringCost}.");
    }

    public void OnDateModified(DateTime gameDateTime) {
        this.gameDateTime = gameDateTime;
        // World Events
        List<WorldEvent> triggeredEvents = eventsController.OnGameDateChanged(gameDateTime, playerCompany);
        foreach (WorldEvent triggeredEvent in triggeredEvents) {
            hudController.OnEventTriggered(triggeredEvent);
        }
        // World News
        News latestNews = newsController.OnGameDateChanged(gameDateTime);
        if (latestNews != null)
            hudController.PushLatestNews(latestNews);
        // HUD
        hudController.OnDateChanged(gameDateTime);
    }

    public void OnPlayerCompanyModified() {
        eventsController.OnPlayerCompanyChanged(gameDateTime, playerCompany);
        hudController.OnCompanyChanged(playerCompany);
    }

    public void OnProjectStarted(Project newProject) {
        playerCompany.StartProject(newProject, gameDateTime);
        hudController.CanStartNewProject(false);
    }

    public void OnProjectCompleted(GameDevCompany company, Project project) {
        if (project.Type() == Project.ProjectType.GameProject)
            engineFeaturesController.CheckFeatures(eventsController,
                gameDateTime, company);
        hudController.CanStartNewProject(true);
    }

    public void OnConstructionStarted(float constructionCost) {
        world.OnConstructionStarted(constructionCost);
    }
}
