using System;
using Database;
using UnityEngine;
using UnityEngine.UI;

public class NewsBarPanel : MonoBehaviour {
    [SerializeField] [UnityEngine.Range(1, 31)] private float delayInDays = 5f;
    private News latestNews;

    public void UpdateDisplayedNews(News news) {
        latestNews = news;
        transform.Find("NewsBarText").GetComponent<Text>().text = latestNews.TextEnglish;
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
