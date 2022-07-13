﻿using UnityEngine;
using UnityEngine.UI;

namespace Pinnacle {
  public class LabelRow {
    public GameObject Row { get; private set; }
    public Text Label { get; private set; }

    public LabelRow(Transform parentTransform) {
      Row = CreateChildRow(parentTransform);
      Label = CreateChildLabel(Row.transform).Text();
    }

    GameObject CreateChildRow(Transform parentTransform) {
      GameObject row = new("Row", typeof(RectTransform));
      row.SetParent(parentTransform);

      row.AddComponent<HorizontalLayoutGroup>()
          .SetChildControl(width: true, height: true)
          .SetChildForceExpand(width: false, height: false)
          .SetPadding(left: 8, right: 8, top: 2, bottom: 2)
          .SetSpacing(12f)
          .SetChildAlignment(TextAnchor.MiddleCenter);

      return row;
    }

    GameObject CreateChildLabel(Transform parentTransform) {
      GameObject label = UIBuilder.CreateLabel(parentTransform);
      label.SetName("Label");

      label.Text()
          .SetAlignment(TextAnchor.MiddleLeft)
          .SetText("Label");

      label.AddComponent<LayoutElement>()
          .SetPreferred(width: 75f);

      return label;
    }
  }
}