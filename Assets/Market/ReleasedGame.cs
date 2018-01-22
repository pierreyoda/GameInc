using System;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class ReleasedGame {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private int reviewerScore;
    [SerializeField] private int usersScore;

    public ReleasedGame(string name, int reviewerScore, int usersScore) {
        Assert.IsTrue(0 <= reviewerScore && reviewerScore <= 100);
        Assert.IsTrue(0 <= usersScore && usersScore <= 100);
        this.name = name;
        this.reviewerScore = reviewerScore;
        this.usersScore = usersScore;
    }
}
