using UnityEngine;

namespace TrashRush.Game
{
    public sealed class WeaponRenderController : MonoBehaviour
    {
        [SerializeField] private Transform _weaponPoint;

        private GameObject _currentWeaponInstance;

        public WeaponData CurrentWeaponData { get; private set; }

        private void OnDestroy()
        {
            Clear();
        }

        public void Show(WeaponData weaponData)
        {
            Clear();
            CurrentWeaponData = weaponData;

            var weaponPrefab = weaponData != null ? weaponData.WeaponPrefab : null;

            if (_weaponPoint == null || weaponPrefab == null)
            {
                return;
            }

            _currentWeaponInstance = Instantiate(weaponPrefab, _weaponPoint);
            _currentWeaponInstance.transform.localPosition = Vector3.zero;
            _currentWeaponInstance.transform.localRotation = weaponPrefab.transform.localRotation;
            _currentWeaponInstance.transform.localScale = weaponPrefab.transform.localScale;
        }

        public void Clear()
        {
            CurrentWeaponData = null;

            if (_currentWeaponInstance == null)
            {
                return;
            }

            Destroy(_currentWeaponInstance);
            _currentWeaponInstance = null;
        }
    }
}
