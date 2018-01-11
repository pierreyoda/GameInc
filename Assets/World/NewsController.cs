using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using UnityEngine;

public class NewsController : MonoBehaviour {
	[SerializeField] private List<News> news;

	public void InitNews(List<News> allNews, DateTime startingDate) {
		// sort by date
		news = new List<News>(allNews.OrderBy(n => n.Date));
		news.RemoveAll(n => n.Date < startingDate.Date);
	}

	public News OnGameDateChanged(DateTime date) {
		if (news.Count == 0) return null;
		News latestNews = news.First();
		if (latestNews.Date.Date == date.Date) {
			news.RemoveAt(0);
			return latestNews;
		}
		return null;
	}
}
