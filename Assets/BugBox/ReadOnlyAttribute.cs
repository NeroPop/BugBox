using UnityEngine;

namespace CustomAttributes
{
    public class ReadOnlyAttribute : PropertyAttribute { }

    //This script should not be placed in the Editor folder and gives the [ReadOnly] attribute which can be used to make any variable appear grayed out and uneditable in the Inspector.
}