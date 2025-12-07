using System.Collections;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _reloadTime = 2f;
    [SerializeField] private Transform _bulletSpawnTransform;
    private Transform _Dynamic;
    private bool _isReloading;

    private void Awake()
    {
        _Dynamic = GameObject.Find("_Dynamic").transform;
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "Enemy")
        {
            if(!_isReloading)
                Shoot(other.transform);
        }
    }
    private void Shoot(Transform targetTransform)
    {
        GameObject bullet = Instantiate(_bulletPrefab, _bulletSpawnTransform);
        bullet.transform.SetParent(_Dynamic);
        bullet.GetComponent<Bullet>().target = targetTransform;
        StartCoroutine(StartReload());
    }
    
    private IEnumerator StartReload()
    {
        _isReloading = true;
        yield return new WaitForSeconds(_reloadTime);
        _isReloading = false;
    }
}