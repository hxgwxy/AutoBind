using UnityEngine.UI;
using TMPro;

public partial class TestAutoBind2
{
	private AutoBindComponent _AutoBindComponent;
	private AutoBindComponent m_AutoBindComponent
    {
	    get
	    {
		    if (_AutoBindComponent == null)
		    {
			    var comps = gameObject.GetComponents<AutoBindComponent>();
			    foreach (var comp in comps)
			    {
				    if (comp.GeneraterComponent.Equals(this))
				    {
					    _AutoBindComponent = comp;
					    break;
				    }
			    }
		    }
		    return _AutoBindComponent;
	    }
    }
	private Image @Image_Img => (Image)m_AutoBindComponent.m_BindDatas[0].BindComp;
	private TextMeshProUGUI @Text_TMP_TextMeshProUGUI => (TextMeshProUGUI)m_AutoBindComponent.m_BindDatas[1].BindComp;
}
