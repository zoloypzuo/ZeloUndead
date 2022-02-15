using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// ----------------------------------------------------------------------------------
// NOTE	:	This class has been updated since the video to fix a timing issue
//			caused by performing the interpolation in fixed update. We now
//			do it in update instead.
// ----------------------------------------------------------------------------------
public class AISoundEmitter : MonoBehaviour 
{
	// Inspector Assigned
	[SerializeField] private float	_decayRate	= 1.0f;

	// Internal
	private SphereCollider	_collider 			= null;
	private float			_srcRadius			= 0.0f;
	private float			_tgtRadius			= 0.0f;
	private float			_interpolator		= 0.0f;
	private float			_interpolatorSpeed	=	0.0f;
	private float			_currentRadius		=	0.0f;

	// Use this for initialization
	void Awake () 
	{
		// Cache Collider Reference
		_collider = GetComponent<SphereCollider>();
		if (!_collider) return;

		// Set Radius Values
		_srcRadius = _tgtRadius = _collider.radius;

		// Setup Interpolator
		_interpolator = 0.0f;
		if (_decayRate>0.02f)
			_interpolatorSpeed = 1.0f/_decayRate;
		else
			_interpolatorSpeed = 0.0f;
	}
	
	void FixedUpdate()
	{
		if (!_collider) return;
		_collider.radius = _currentRadius;
		if (_collider.radius<Mathf.Epsilon) _collider.enabled = false;
		else                         		_collider.enabled = true;
	}

	void Update()
	{
		_interpolator = Mathf.Clamp01( _interpolator+Time.deltaTime*_interpolatorSpeed);
		_currentRadius = Mathf.Lerp(_srcRadius,_tgtRadius,_interpolator);
	}

	public void SetRadius( float newRadius, bool instantResize = false )
	{
		if (!_collider || newRadius==_tgtRadius ) return;

		_srcRadius 		=  _currentRadius = (instantResize || newRadius>_currentRadius)?newRadius:_currentRadius;
		_tgtRadius 		= newRadius;
		_interpolator 	= 0.0f;

	}
}
