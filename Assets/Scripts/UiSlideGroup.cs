using UnityEngine;

public static class UiSlideGroup
{
    private static AnimatorToggleButton currentOpen;

    public static void RequestOpen(AnimatorToggleButton requester)
    {
        if (currentOpen && currentOpen != requester)
            currentOpen.Close();

        currentOpen = requester;
    }

    public static void NotifyClosed(AnimatorToggleButton requester)
    {
        if (currentOpen == requester) currentOpen = null;
    }
}
