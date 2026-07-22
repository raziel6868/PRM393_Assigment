namespace MyFSchool.Domain.School;

public enum AnnouncementAudience
{
    SchoolWide,
    Class,
    Teacher,
    Parent,
    Student
}

public enum DeliveryChannel
{
    PortalApp,
    Email
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Failed
}
