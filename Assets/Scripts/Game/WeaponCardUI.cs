using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrashRush.Game
{
    [RequireComponent(typeof(Button))]
    public sealed class WeaponCardUI : MonoBehaviour
    {
        [SerializeField] private Image _weaponImage;
        [SerializeField] private TMP_Text _weaponNameLabel;
        [SerializeField] private GameObject _selectedIndicator;
        [SerializeField] private Button _button;

        private Action<WeaponCardUI> _clicked;

        public WeaponData WeaponData { get; private set; }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Setup(WeaponData weaponData, Action<WeaponCardUI> clicked)
        {
            ResolveReferences();

            WeaponData = weaponData;
            _clicked = clicked;

            if (_weaponImage != null && weaponData != null && weaponData.WeaponSprite != null)
            {
                _weaponImage.sprite = weaponData.WeaponSprite;
            }

            if (_weaponNameLabel != null)
            {
                _weaponNameLabel.text = weaponData != null ? weaponData.DisplayName : string.Empty;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(selected);
            }
        }

        private void OnClicked()
        {
            _clicked?.Invoke(this);
        }

        private void ResolveReferences()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

        }
    }
}
