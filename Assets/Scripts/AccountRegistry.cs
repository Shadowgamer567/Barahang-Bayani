using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class AccountRegistry
{
    public List<AccountData> accounts = new();
}
