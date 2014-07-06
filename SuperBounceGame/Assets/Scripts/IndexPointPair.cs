using UnityEngine;
using System;

public class IndexPointPair
{
	public int index = 0;
	public Vector3 point;

	public IndexPointPair(int index, Vector3 point)
	{
		this.index = index;
		this.point = point;
	}

	public IndexPointPair(IndexPointPair pointPair)
	{
		this.index = pointPair.index;
		this.point =  pointPair.point;
	}

	public string ToDebugString()
	{
		return index + ": " + point.ToString();
	}
}