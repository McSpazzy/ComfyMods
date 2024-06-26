﻿namespace Contextual;

using ComfyLib;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public sealed class ContextMenuItem {
  public readonly GameObject Container;
  public readonly RectTransform RectTransform;
  public readonly LayoutElement LayoutElement;
  public readonly Image Background;
  public readonly TMP_Text Label;
  public readonly Button Button;

  public ContextMenuItem(Transform parentTransform) {
    Container = CreateContainer(parentTransform);
    RectTransform = Container.GetComponent<RectTransform>();
    LayoutElement = Container.GetComponent<LayoutElement>();
    Background = Container.GetComponent<Image>();
    Label = CreateLabel(Container.transform);
    Button = CreateButton(Container);
  }

  public void SetText(string text) {
    Label.text = text;
    Label.ForceMeshUpdate(ignoreActiveState: true);

    LayoutElement.SetPreferred(height: Label.preferredHeight + 10f);
  }

  static GameObject CreateContainer(Transform parentTransform) {
    GameObject container = new("ContextMenuItem", typeof(RectTransform));
    container.transform.SetParent(parentTransform, worldPositionStays: true);

    container.AddComponent<Image>()
        .SetSprite(UIResources.GetSprite("item_background"))
        .SetType(Image.Type.Sliced)
        .SetColor(new(1f, 1f, 1f, 0.759f))
        .SetRaycastTarget(true);

    container.GetComponent<RectTransform>()
        .SetAnchorMin(Vector2.zero)
        .SetAnchorMax(Vector2.one)
        .SetPivot(new(0.5f, 0.5f))
        .SetPosition(Vector2.zero)
        .SetSizeDelta(Vector2.zero);

    container.AddComponent<LayoutElement>()
        .SetFlexible(width: 1f)
        .SetPreferred(height: 35f);

    return container;
  }

  static TMP_Text CreateLabel(Transform parentTransform) {
    TMP_Text label = UIBuilder.CreateTMPLabel(parentTransform);
    label.transform.SetParent(parentTransform, worldPositionStays: false);

    label.rectTransform
        .SetAnchorMin(Vector2.zero)
        .SetAnchorMax(Vector2.one)
        .SetPivot(new(0.5f, 0.5f))
        .SetPosition(Vector2.zero)
        .SetSizeDelta(new(-10f, 0f));

    label
        .SetFontSize(16f)
        .SetAlignment(TextAlignmentOptions.Left)
        .SetTextWrappingMode(TextWrappingModes.NoWrap)
        .SetOverflowMode(TextOverflowModes.Ellipsis)
        .SetText("...");

    return label;
  }

  static Button CreateButton(GameObject container) {
    Button button = container.AddComponent<Button>();

    button
        .SetTransition(Selectable.Transition.ColorTint)
        .SetColors(
            new ColorBlock() {
              normalColor = new(0.353f, 0.35f, 0.35f, 1f),
              highlightedColor = new(0.625f, 0.625f, 0.625f, 1f),
              pressedColor = new(0.890f, 0.890f, 0.890f, 1f),
              selectedColor = new(0.625f, 0.625f, 0.625f, 1f),
              disabledColor = new(0.345f, 0.345f, 0.345f, 0.5f),
              colorMultiplier = 1f,
              fadeDuration = 0.1f
            });

    return button;
  }
}
