﻿using System.Linq;

namespace Atata
{
    /// <summary>
    /// Represents the base class for the popup window page objects.
    /// </summary>
    /// <typeparam name="TOwner">The type of the owner page object.</typeparam>
    [PageObjectDefinition(ComponentTypeName = "window", IgnoreNameEndings = "PopupWindow,Window,Popup")]
    public abstract class PopupWindow<TOwner> : PageObject<TOwner>
        where TOwner : PopupWindow<TOwner>
    {
        protected PopupWindow(params string[] windowTitleValues)
        {
            WindowTitleValues = windowTitleValues;
        }

        /// <summary>
        /// Gets or sets the window title values.
        /// </summary>
        protected string[] WindowTitleValues { get; set; }

        /// <summary>
        /// Gets or sets the match that should be used for the window search by the title. The default value is <c>TermMatch.Equals</c>.
        /// </summary>
        protected TermMatch WindowTitleMatch { get; set; } = TermMatch.Equals;

        /// <summary>
        /// Gets a value indicating whether window can be found by window title.
        /// </summary>
        protected bool CanFindByWindowTitle
        {
            get { return WindowTitleValues != null && WindowTitleValues.Any() && WindowTitleMatch != TermMatch.Inherit; }
        }

        protected internal override void ApplyMetadata(UIComponentMetadata metadata)
        {
            base.ApplyMetadata(metadata);

            WindowTitleAttribute titleAttribute = metadata.GetFirstOrDefaultAttribute<WindowTitleAttribute>();
            if (titleAttribute != null)
            {
                WindowTitleValues = titleAttribute.GetActualValues(ComponentName);
                WindowTitleMatch = titleAttribute.Match;
            }
        }
    }
}
