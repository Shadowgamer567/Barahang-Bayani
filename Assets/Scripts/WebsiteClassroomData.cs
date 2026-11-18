using System;

[Serializable]
public class WebsiteClassroomListResult
{
    public bool success;
    public WebsiteClassroomData[] classrooms;
}

[Serializable]
public class WebsiteClassroomData
{
    public string id;
    public string name;
    public bool isOwner;
    public WebsiteClassroomPermissions permissions;
}

[Serializable]
public class WebsiteClassroomPermissions
{
    public bool canManageClassroom;
    public bool canManageStudents;
    public bool canViewProgress;
    public bool canManageQuizzes;
}

[Serializable]
public class WebsiteCreateClassroomRequest
{
    public string name;
}

[Serializable]
public class WebsiteCreateClassroomResult
{
    public bool success;
    public string message;
    public WebsiteCreatedClassroom classroom;
}

[Serializable]
public class WebsiteCreatedClassroom
{
    public string id;
    public string name;
    public string accessCode;
}
