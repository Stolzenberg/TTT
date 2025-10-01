using Sandbox;
using System;

namespace Editor;

public class SplineEditorTool : EditorTool<SplineComponent>
{

	public override void OnEnabled()
	{
		window = new SplineToolWindow();
		AddOverlay( window, TextFlag.RightBottom, 10 );
	}

	public override void OnUpdate()
	{
		window.ToolUpdate();
	}

	public override void OnDisabled()
	{
		window.OnDisabled();
	}

	public override void OnSelectionChanged()
	{
		var target = GetSelectedComponent<SplineComponent>();
		window.OnSelectionChanged( target );
	}

	private SplineToolWindow window = null;
}

class SplineToolWindow : WidgetWindow
{
	SplineComponent targetComponent;

	static bool IsClosed = false;

	ControlWidget inTangentControl;
	ControlWidget outTangentControl;

	public SplineToolWindow()
	{
		ContentMargins = 0;
		Layout = Layout.Column();
		MaximumWidth = 500;
		MinimumWidth = 300;

		Rebuild();
	}

	void Rebuild()
	{
		Layout.Clear( true );
		Layout.Margin = 0;
		Icon = IsClosed ? "" : "route";
		UpdateWindowTitle();
		IsGrabbable = !IsClosed;

		if ( IsClosed )
		{
			var closedRow = Layout.AddRow();
			closedRow.Add( new IconButton( "route", () => { IsClosed = false; Rebuild(); } ) { ToolTip = "Open Spline Point Editor", FixedHeight = HeaderHeight, FixedWidth = HeaderHeight, Background = Color.Transparent } );
			MinimumWidth = 0;
			return;
		}

		MinimumWidth = 400;

		var headerRow = Layout.AddRow();
		headerRow.AddStretchCell();
		headerRow.Add( new IconButton( "info" )
		{
			ToolTip = "Controls to edit the spline points.\nIn addition to modifying the properties in the control sheet, you can also use the 3D Gizmos.\nClicking on the spline between points will split the spline at that position.\nHolding shift while dragging a point's position will drag out a new point.",
			FixedHeight = HeaderHeight,
			FixedWidth = HeaderHeight,
			Background = Color.Transparent
		} );
		headerRow.Add( new IconButton( "close", CloseWindow ) { ToolTip = "Close Editor", FixedHeight = HeaderHeight, FixedWidth = HeaderHeight, Background = Color.Transparent } );

		if ( targetComponent.IsValid() )
		{
			var tangentMode = this.GetSerialized().GetProperty( nameof( _selectedPointTangentMode ) );
			var roll = this.GetSerialized().GetProperty( nameof( _selectedPointRoll ) );
			var scale = this.GetSerialized().GetProperty( nameof( _selectedPointScale ) );
			var up = this.GetSerialized().GetProperty( nameof( _selectedPointUp ) );

			var controlSheet = new ControlSheet();

			// TODO find out why tf this isnt working
			controlSheet.AddRow( tangentMode );
			controlSheet.AddRow( this.GetSerialized().GetProperty( nameof( _selectedPointPosition ) ) );
			inTangentControl = controlSheet.AddRow( this.GetSerialized().GetProperty( nameof( _selectedPointIn ) ) );
			outTangentControl = controlSheet.AddRow( this.GetSerialized().GetProperty( nameof( _selectedPointOut ) ) );
			controlSheet.AddGroup( "Advanced", new[] { roll, scale, up } );

			var row = Layout.Row();
			row.Spacing = 16;
			row.Margin = 8;
			row.Add( new IconButton( "skip_previous", () =>
			{
				SelectedPointIndex = Math.Max( 0, SelectedPointIndex - 1 );

				UpdateWindowTitle();
				Focus();
			} )
			{ ToolTip = "Go to previous point " } );
			row.Add( new IconButton( "skip_next", () =>
			{
				SelectedPointIndex = Math.Min( targetComponent.Spline.PointCount - 1, SelectedPointIndex + 1 );

				UpdateWindowTitle();
				Focus();
			} )
			{ ToolTip = "Go to next point" } );
			row.Add( new IconButton( "delete", () =>
			{
				using ( SceneEditorSession.Active.UndoScope( "Delete Spline Point" ).WithComponentChanges( targetComponent ).Push() )
				{
					targetComponent.Spline.RemovePoint( SelectedPointIndex );
					SelectedPointIndex = Math.Max( 0, SelectedPointIndex - 1 );
				}

				UpdateWindowTitle();
				Focus();
			} )
			{ ToolTip = "Delete point" } );
			row.Add( new IconButton( "add", () =>
			{
				using ( SceneEditorSession.Active.UndoScope( "Added Spline Point" ).WithComponentChanges( targetComponent ).Push() )
				{
					if ( SelectedPointIndex == targetComponent.Spline.PointCount - 1 )
					{
						targetComponent.Spline.InsertPoint( SelectedPointIndex + 1, _selectedPoint with { Position = _selectedPoint.Position + targetComponent.Spline.SampleAtDistance( targetComponent.Spline.GetDistanceAtPoint( SelectedPointIndex ) ).Tangent * 200 } );
					}
					else
					{
						targetComponent.Spline.AddPointAtDistance( (targetComponent.Spline.GetDistanceAtPoint( SelectedPointIndex ) + targetComponent.Spline.GetDistanceAtPoint( SelectedPointIndex + 1 )) / 2, true );
						// TOOD infer tangent modes???
					}
				}

				SelectedPointIndex++;

				UpdateWindowTitle();
				Focus();
			} )
			{ ToolTip = "Insert point after curent point.\nYou can also hold shift while dragging a point to create a new point." } );

			controlSheet.AddLayout( row );

			Layout.Add( controlSheet );

			ToggleTangentInput();
		}


		Layout.Margin = 4;
	}

	void UpdateWindowTitle()
	{
		WindowTitle = IsClosed ? "" : $"Spline Point [{SelectedPointIndex}] Editor - {targetComponent?.GameObject?.Name ?? ""}";
	}

	void CloseWindow()
	{
		IsClosed = true;
		// TODO internal ?
		// Release();
		Rebuild();
		Position = Parent.Size - 32;
	}

	public void ToolUpdate()
	{
		if ( !targetComponent.IsValid() )
			return;

		DrawGizmos();
	}

	public void OnSelectionChanged( SplineComponent spline )
	{
		if ( targetComponent.IsValid() )
		{
			targetComponent.ShouldRenderGizmos = true;
		}

		targetComponent = spline;

		targetComponent.ShouldRenderGizmos = false;

		Rebuild();
	}

	public void OnDisabled()
	{
		if ( targetComponent.IsValid() )
		{
			targetComponent.ShouldRenderGizmos = true;
		}
	}

	private void ToggleTangentInput()
	{
		if ( _selectedPoint.Mode == Spline.HandleMode.Auto || _selectedPoint.Mode == Spline.HandleMode.Linear )
		{
			inTangentControl.Enabled = false;
			outTangentControl.Enabled = false;
		}
		else
		{
			inTangentControl.Enabled = true;
			outTangentControl.Enabled = true;
		}
	}

	int SelectedPointIndex
	{
		get => _selectedPointIndex;
		set
		{
			_selectedPointIndex = value;
			ToggleTangentInput();
		}
	}

	int _selectedPointIndex = 0;

	Spline.Point _selectedPoint
	{
		get => SelectedPointIndex > targetComponent.Spline.PointCount - 1 ? new Spline.Point() : targetComponent.Spline.GetPoint( SelectedPointIndex );
		set
		{
			using ( SceneEditorSession.Active.UndoScope( "Added Spline Point" ).WithComponentChanges( targetComponent ).Push() )
			{
				targetComponent.Spline.UpdatePoint( SelectedPointIndex, value );
			}
		}
	}

	[Title( "Position" )]
	Vector3 _selectedPointPosition
	{
		get => SelectedPointIndex > targetComponent.Spline.PointCount - 1 ? Vector3.Zero : _selectedPoint.Position;
		set
		{
			using ( SceneEditorSession.Active.UndoScope( "Updated Spline Point" ).WithComponentChanges( targetComponent ).Push() )
			{
				_selectedPoint = _selectedPoint with { Position = value };
			}
		}
	}

	[Title( "In" )]
	Vector3 _selectedPointIn
	{
		get => SelectedPointIndex > targetComponent.Spline.PointCount - 1 ? Vector3.Zero : _selectedPoint.In;
		set
		{
			using ( SceneEditorSession.Active.UndoScope( "Updated Spline Point" ).WithComponentChanges( targetComponent ).Push() )
			{
				_selectedPoint = _selectedPoint with { In = value };
			}
		}
	}

	[Title( "Out" )]
	Vector3 _selectedPointOut
	{
		get => SelectedPointIndex > targetComponent.Spline.PointCount - 1 ? Vector3.Zero : _selectedPoint.Out;
		set
		{
			using ( SceneEditorSession.Active.UndoScope( "Updated Spline Point" ).WithComponentChanges( targetComponent ).Push() )
			{
				_selectedPoint = _selectedPoint with { Out = value };
			}
		}
	}


	// TODO this is temp until i can figure out why editor library refuses to create a controlsheet row for the engine type
	/// <summary>
	/// Describes how the spline should behave when entering/leaving a point.
	/// </summary>
	public enum HandleModeTemp
	{
		/// <summary>
		/// Handle positions are calculated automatically
		/// based on the location of adjacent points.
		/// </summary>
		[Icon( "auto_fix_high" )]
		Auto,
		/// <summary>
		/// Handle positions are set to zero, leading to a sharp corner.
		/// </summary>
		[Icon( "show_chart" )]
		Linear,
		/// <summary>
		/// The In and Out handles are user set, but are linked (mirrored).
		/// </summary>
		[Icon( "open_in_full" )]
		Mirrored,
		/// <summary>
		/// The In and Out handle are user set and operate independently.
		/// </summary>
		[Icon( "call_split" )]
		Split,
	}

	[Title( "Tangent Mode" )]
	HandleModeTemp _selectedPointTangentMode
	{
		get => SelectedPointIndex > targetComponent.Spline.PointCount - 1 ? (HandleModeTemp)Spline.HandleMode.Auto : (HandleModeTemp)_selectedPoint.Mode;
		set
		{
			using ( SceneEditorSession.Active.UndoScope( "Updated Spline Point" ).WithComponentChanges( targetComponent ).Push() )
			{
				_selectedPoint = _selectedPoint with { Mode = (Spline.HandleMode)value };
				ToggleTangentInput();
			}
		}
	}

	[Title( "Roll (Degrees)" )]
	float _selectedPointRoll
	{
		get => SelectedPointIndex > targetComponent.Spline.PointCount - 1 ? 0f : _selectedPoint.Roll;
		set
		{
			using ( SceneEditorSession.Active.UndoScope( "Updated Spline Point" ).WithComponentChanges( targetComponent ).Push() )
			{
				_selectedPoint = _selectedPoint with { Roll = value };
			}
		}
	}

	[Title( "Up Vector" )]
	Vector3 _selectedPointUp
	{
		get => SelectedPointIndex > targetComponent.Spline.PointCount - 1 ? Vector3.Zero : _selectedPoint.Up;
		set
		{
			using ( SceneEditorSession.Active.UndoScope( "Updated Spline Point" ).WithComponentChanges( targetComponent ).Push() )
			{
				_selectedPoint = _selectedPoint with { Up = value };
			}
		}
	}

	[Title( "Scale (_, Width, Height)" )]
	Vector3 _selectedPointScale
	{
		get => SelectedPointIndex > targetComponent.Spline.PointCount - 1 ? 0f : _selectedPoint.Scale;
		set
		{
			using ( SceneEditorSession.Active.UndoScope( "Updated Spline Point" ).WithComponentChanges( targetComponent ).Push() )
			{
				_selectedPoint = _selectedPoint with { Scale = value };
			}
		}
	}

	bool _inTangentSelected = false;

	bool _outTangentSelected = false;

	bool _draggingOutNewPoint = false;

	bool _moveInProgress = false;

	List<Vector3> polyLine = new();

	IDisposable _movementUndoScope = null;

	void DrawGizmos()
	{

		using ( Gizmo.Scope( "spline_editor", targetComponent.WorldTransform ) )
		{
			targetComponent.Spline.ConvertToPolyline( ref polyLine );

			for ( var i = 0; i < polyLine.Count - 1; i++ )
			{
				using ( Gizmo.Scope( "segment" + i ) )
				{
					using ( Gizmo.Hitbox.LineScope() )
					{
						Gizmo.Draw.LineThickness = 2f;

						Gizmo.Hitbox.AddPotentialLine( polyLine[i], polyLine[i + 1], Gizmo.Draw.LineThickness * 2f );

						Gizmo.Draw.Line( polyLine[i], polyLine[i + 1] );

						if ( Gizmo.IsHovered && Gizmo.HasMouseFocus )
						{
							Gizmo.Draw.Color = Color.Orange;
							Vector3 point_on_line;
							Vector3 point_on_ray;
							if ( !new Line( polyLine[i], polyLine[i + 1] ).ClosestPoint(
									Gizmo.CurrentRay.ToLocal( Gizmo.Transform ), out point_on_line, out point_on_ray ) )
								return;

							// It would be slighlty more efficient to use Spline.Utils directly,
							// but doggfoding the simplified component API ensures a user of that one would also have the ability to built a spline editor
							var hoverSample = targetComponent.Spline.SampleAtClosestPosition( point_on_line );

							using ( Gizmo.Scope( "hover_handle", new Transform( point_on_line,
									   Rotation.LookAt( hoverSample.Tangent ) ) ) )
							{
								using ( Gizmo.GizmoControls.PushFixedScale() )
								{
									Gizmo.Draw.SolidBox( BBox.FromPositionAndSize( Vector3.Zero, 2f ) );
								}
							}

							if ( Gizmo.HasClicked && Gizmo.Pressed.This )
							{
								using ( SceneEditorSession.Active.UndoScope( "Added spline point" ).WithComponentChanges( targetComponent ).Push() )
								{
									var newPointIndex = targetComponent.Spline.AddPointAtDistance( hoverSample.Distance, true );
									SelectedPointIndex = newPointIndex;
									_inTangentSelected = false;
									_outTangentSelected = false;
								}
							}
						}
					}
				}
			}

			// position location
			var positionGizmoLocation = _selectedPoint.Position;
			if ( _inTangentSelected )
			{
				positionGizmoLocation += _selectedPoint.In;
			}

			if ( _outTangentSelected )
			{
				positionGizmoLocation += _selectedPoint.Out;
			}

			if ( !Gizmo.IsShiftPressed )
			{
				_draggingOutNewPoint = false;
			}

			using ( Gizmo.Scope( "position", new Transform( positionGizmoLocation ) ) )
			{
				_moveInProgress = false;
				if ( Gizmo.Control.Position( "spline_control_", Vector3.Zero, out var delta ) )
				{
					_moveInProgress = true;
					_movementUndoScope ??= SceneEditorSession.Active.UndoScope( "Moved spline point" ).WithComponentChanges( targetComponent ).Push();
					if ( _inTangentSelected )
					{
						MoveSelectedPointInTanget( delta );
					}
					else if ( _outTangentSelected )
					{
						MoveSelectedPointOutTanget( delta );
					}
					else
					{
						if ( Gizmo.IsShiftPressed && !_draggingOutNewPoint )
						{
							_draggingOutNewPoint = true;

							var currentPoint = targetComponent.Spline.GetPoint( SelectedPointIndex );

							targetComponent.Spline.InsertPoint( SelectedPointIndex + 1, currentPoint );


							SelectedPointIndex++;
						}
						else
						{
							MoveSelectedPoint( delta );
						}
					}
				}
				if ( !_moveInProgress && Gizmo.WasLeftMouseReleased )
				{
					_movementUndoScope?.Dispose();
				}
			}

			for ( var i = 0; i < targetComponent.Spline.PointCount; i++ )
			{
				if ( !targetComponent.Spline.IsLoop || i != targetComponent.Spline.SegmentCount )
				{
					var splinePoint = targetComponent.Spline.GetPoint( i );

					using ( Gizmo.Scope( "point_controls" + i, new Transform( splinePoint.Position ) ) )
					{
						Gizmo.Draw.IgnoreDepth = true;

						using ( Gizmo.Scope( "position" ) )
						{
							using ( Gizmo.GizmoControls.PushFixedScale() )
							{
								Gizmo.Hitbox.DepthBias = 0.1f;
								Gizmo.Hitbox.BBox( BBox.FromPositionAndSize( Vector3.Zero, 2f ) );

								if ( Gizmo.IsHovered || i == SelectedPointIndex &&
									(!_inTangentSelected && !_outTangentSelected) )
								{
									Gizmo.Draw.Color = Color.Orange;
								}

								Gizmo.Draw.SolidBox( BBox.FromPositionAndSize( Vector3.Zero, 2f ) );

								if ( Gizmo.HasClicked && Gizmo.Pressed.This )
								{
									SelectedPointIndex = i;
									_inTangentSelected = false;
									_outTangentSelected = false;
								}
							}
						}

						Gizmo.Draw.Color = Color.White;


						if ( SelectedPointIndex == i )
						{
							Gizmo.Draw.LineThickness = 0.8f;
							using ( Gizmo.Scope( "in_tangent", new Transform( splinePoint.In ) ) )
							{

								if ( (_selectedPointTangentMode == HandleModeTemp.Mirrored || _selectedPointTangentMode == HandleModeTemp.Auto) && (_inTangentSelected || _outTangentSelected) )
								{
									Gizmo.Draw.Color = Color.Orange;
								}

								Gizmo.Draw.Line( -splinePoint.In, Vector3.Zero );

								using ( Gizmo.GizmoControls.PushFixedScale() )
								{
									if ( _selectedPointTangentMode != HandleModeTemp.Linear )
									{
										Gizmo.Hitbox.DepthBias = 0.1f;
										Gizmo.Hitbox.BBox( BBox.FromPositionAndSize( Vector3.Zero, 2f ) );

										if ( Gizmo.IsHovered || _inTangentSelected )
										{
											Gizmo.Draw.Color = Color.Orange;
										}

										Gizmo.Draw.SolidBox( BBox.FromPositionAndSize( Vector3.Zero, 2f ) );

										if ( Gizmo.HasClicked && Gizmo.Pressed.This )
										{
											SelectedPointIndex = i;
											_outTangentSelected = false;
											_inTangentSelected = true;
										}
									}

								}
							}

							using ( Gizmo.Scope( "out_tangent", new Transform( splinePoint.Out ) ) )
							{
								if ( (_selectedPointTangentMode == HandleModeTemp.Mirrored || _selectedPointTangentMode == HandleModeTemp.Auto) && (_inTangentSelected || _outTangentSelected) )
								{
									Gizmo.Draw.Color = Color.Orange;
								}

								Gizmo.Draw.Line( -splinePoint.Out, Vector3.Zero );

								using ( Gizmo.GizmoControls.PushFixedScale() )
								{
									if ( _selectedPointTangentMode != HandleModeTemp.Linear )
									{

										Gizmo.Hitbox.BBox( BBox.FromPositionAndSize( Vector3.Zero, 2f ) );

										if ( Gizmo.IsHovered || _outTangentSelected )
										{
											Gizmo.Draw.Color = Color.Orange;
										}

										Gizmo.Draw.SolidBox( BBox.FromPositionAndSize( Vector3.Zero, 2f ) );

										if ( Gizmo.HasClicked && Gizmo.Pressed.This )
										{
											SelectedPointIndex = i;
											_inTangentSelected = false;
											_outTangentSelected = true;
										}

									}
								}
							}
						}
					}
				}
			}
		}
	}

	private void MoveSelectedPoint( Vector3 delta )
	{
		var updatedPoint = _selectedPoint with { Position = _selectedPoint.Position + delta };
		targetComponent.Spline.UpdatePoint( SelectedPointIndex, updatedPoint );
	}

	private void MoveSelectedPointInTanget( Vector3 delta )
	{
		var updatedPoint = _selectedPoint;
		updatedPoint.In += delta;
		if ( _selectedPointTangentMode == HandleModeTemp.Auto )
		{
			updatedPoint = _selectedPoint with { Mode = Spline.HandleMode.Mirrored };
		}
		if ( _selectedPointTangentMode == HandleModeTemp.Mirrored )
		{
			updatedPoint.Out = -updatedPoint.In;
		}
		targetComponent.Spline.UpdatePoint( SelectedPointIndex, updatedPoint );
	}

	private void MoveSelectedPointOutTanget( Vector3 delta )
	{
		var updatedPoint = _selectedPoint;
		updatedPoint.Out += delta;
		if ( _selectedPointTangentMode == HandleModeTemp.Auto )
		{
			updatedPoint = _selectedPoint with { Mode = Spline.HandleMode.Mirrored };
		}
		if ( _selectedPointTangentMode == HandleModeTemp.Mirrored )
		{
			updatedPoint.In = -updatedPoint.Out;
		}
		targetComponent.Spline.UpdatePoint( SelectedPointIndex, updatedPoint );
	}
}
