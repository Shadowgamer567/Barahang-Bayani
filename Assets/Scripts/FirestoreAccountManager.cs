using Firebase.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreAccountManager : MonoBehaviour
{
    public static async Task UploadAccount(AccountData account)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data["id"] = account.id;
        data["usename"] = account.username;
        data["passwordHash"] = account.passwordHash;
        data["accountType"] = account.accountType.ToString();
        data["assignedStudentIDs"] = account.assignedStudentIDs;

        await FirestoreManager.Instance
            .AccountsCollection()
            .Document(account.id)
            .SetAsync(data);

        Debug.Log("Uploaded Account: " + account.username);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
