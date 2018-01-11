using System;
using Database;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class NewsBarPanel : MonoBehaviour {
    [SerializeField] [UnityEngine.Range(1, 31)] private float delayInDays = 5f;
    [SerializeField] private Text newsBarText;
    private News latestNews;

    private void Start() {
        Assert.IsTrue(delayInDays > 0);
        Assert.IsNotNull(newsBarText);
        newsBarText.text = ""; // clear debug text
    }

    public void UpdateNewsText(News news) {
        latestNews = news;
        newsBarText.text = latestNews.TextEnglish;
    }

    /// <summary>
    /// Update the current date and dismiss the latest news if enough days have passed.
    /// </summary>
    /// <param name="currentDate">Current in-game date.</param>
    /// <returns>False if the news prompt should be dismissed, true otherwise.</returns>
    public bool UpdateDate(DateTime currentDate) {
        if (latestNews == null) return false;
        return latestNews.Date.AddDays(delayInDays) >= currentDate;
    }
}
