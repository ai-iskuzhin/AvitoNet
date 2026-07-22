namespace AvitoNet.Messenger.Models;

/// <summary>
/// Message content types on the wire. Open set (Avito may add more) and some are camelCase, so these are
/// plain string constants rather than an enum — compare against <c>message.Type</c> and keep unknown values.
/// </summary>
public static class AvitoMessageType
{
    public const string Text = "text";
    public const string Image = "image";
    public const string Link = "link";
    public const string Item = "item";
    public const string Location = "location";
    public const string Call = "call";
    public const string AppCall = "appCall";
    public const string Voice = "voice";
    public const string Video = "video";
    public const string File = "file";
    public const string System = "system";
    public const string Deleted = "deleted";
}

/// <summary>Chat type. <c>u2i</c> = about an item, <c>u2u</c> = user-to-user, <c>a2u</c> = Avito-to-user.</summary>
public static class AvitoChatType
{
    public const string UserToItem = "u2i";
    public const string UserToUser = "u2u";
    public const string AvitoToUser = "a2u";
}

/// <summary>Message direction relative to the authenticated account.</summary>
public static class AvitoMessageDirection
{
    public const string In = "in";
    public const string Out = "out";
}
