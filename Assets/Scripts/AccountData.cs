using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public enum AccountType
{
    Student,
    Teacher
}

public enum AccountCardMode
{
    Normal,
    TeacherStudentView
}

[System.Serializable]
public class AccountData
{
    public string id;
    public string username;
    public AccountType accountType;

    public List<string> assignedStudentIDs = new();
}

public static class CurrentAccount
{
    public static AccountData ActiveAccount;
}

