using NUnit.Framework;
using UnityEngine;

public  enum AccountType
{
    Student,
    Teacher
}

[System.Serializable]
public class AccountData
{
    public string id;
    public string username;
    public AccountType accountType;
}

public static class CurrentAccount
{
    public static AccountData ActiveAccount;
}

