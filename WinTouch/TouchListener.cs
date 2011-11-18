﻿#region The MIT License (MIT)
//
// Copyright (c) 2011 Robert Prouse http://www.alteridem.net
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of 
// this software and associated documentation files (the "Software"), to deal in 
// the Software without restriction, including without limitation the rights to use, 
// copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
// Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

#region Using Directives

using System;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;

#endregion

namespace Alteridem.WinTouch
{
    [PermissionSet( SecurityAction.Demand, Name = "FullTrust" )]
    public sealed class TouchListener : NativeWindow
    {
        #region Private Members

        // Saved state
        private Point _lastPanPoint;
        private double _lastRotation;
        private long _lastZoom;

        #endregion

        #region Public Events

        public event EventHandler<PanEventArgs> Pan;
        public event EventHandler<PressAndTapEventArgs> PressAndTap;
        public event EventHandler<RotateEventArgs> Rotate;
        public event EventHandler<TwoFingerTapEventArgs> TwoFingerTap;
        public event EventHandler<ZoomEventArgs> Zoom;

        #endregion

        #region Construction

        public TouchListener( Control parent )
        {
            parent.HandleCreated += OnHandleCreated;
            parent.HandleDestroyed += OnHandleDestroyed;
        }

        #endregion

        #region Private Methods

        private void OnHandleCreated( object sender, EventArgs e )
        {
            // Window is now created, assign handle to NativeWindow.
            var control = sender as Control;
            if ( control != null )
            {
                AssignHandle( control.Handle );
                NativeMethods.SetGestureConfig( control.Handle, 0, GestureConfigurationFlag.GC_ALLGESTURES, 0 );
            }
        }

        private void OnHandleDestroyed( object sender, EventArgs e )
        {
            // Window was destroyed, release hook.
            ReleaseHandle();
        }

        #endregion

        #region WndProc

        /// <summary>
        /// Invokes the default window procedure associated with this window.
        /// </summary>
        /// <param name="m">A <see cref="T:System.Windows.Forms.Message"/> that is associated with the current Windows message.</param>
        [PermissionSet( SecurityAction.Demand, Name = "FullTrust" )]
        protected override void WndProc( ref Message m )
        {
            bool handled = false;

            // Listen for operating system messages
            switch ( m.Msg )
            {
                case WindowMessage.WM_GESTURE:
                    GestureInfo info;
                    if ( NativeMethods.GetGestureInfo( m.LParam, out info ) )
                    {
                        switch ( (GestureId)info.id )
                        {
                            case GestureId.Pan:
                                if ( Pan != null )
                                {
                                    if ( info.Begin )
                                    {
                                        _lastPanPoint = new Point( info.location.x, info.location.y );
                                    }
                                    var args = new PanEventArgs( info, _lastPanPoint );
                                    _lastPanPoint = new Point( info.location.x, info.location.y );
                                    Pan( this, args );
                                    handled = args.Handled;
                                }
                                break;
                            case GestureId.PressAndTap:
                                if ( PressAndTap != null )
                                {
                                    var args = new PressAndTapEventArgs( info );
                                    PressAndTap( this, args );
                                    handled = args.Handled;
                                }
                                break;
                            case GestureId.Rotate:
                                if ( Rotate != null )
                                {
                                    if ( info.Begin )
                                    {
                                        _lastRotation = 0;
                                    }
                                    var args = new RotateEventArgs( info, _lastRotation );
                                    if ( !info.Begin )
                                    {
                                        // First rotation is the angle the fingers are at, so don't use it
                                        _lastRotation = args.TotalAngle;
                                    }
                                    Rotate( this, args );
                                    handled = args.Handled;
                                }
                                break;
                            case GestureId.TwoFingerTap:
                                if ( TwoFingerTap != null )
                                {
                                    var args = new TwoFingerTapEventArgs( info );
                                    TwoFingerTap( this, args );
                                    handled = args.Handled;
                                }
                                break;
                            case GestureId.Zoom:
                                if ( Zoom != null )
                                {
                                    if ( info.Begin )
                                    {
                                        _lastZoom = info.arguments;
                                    }
                                    var args = new ZoomEventArgs( info, _lastZoom );
                                    _lastZoom = args.Distance;
                                    Zoom( this, args );
                                    handled = args.Handled;
                                }
                                break;
                        }
                        if ( handled )
                        {
                            NativeMethods.CloseGestureInfoHandle( m.LParam );
                        }
                    }
                    break;
            }
            if ( !handled )
            {
                base.WndProc( ref m );
            }
        }

        #endregion
    }
}
