/********************************************************************************
 Copyright (C) 2012 Eric Bataille <e.c.p.bataille@gmail.com>

 This program is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program; if not, write to the Free Software
 Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307, USA.
********************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotr
{
    /// <summary>
    /// Enumeration representing the capture state the program is currently in.
    /// </summary>
    enum CaptureState
    {
        /// <summary>
        /// The start of the area is being captured.
        /// </summary>
        CapturingStart,

        /// <summary>
        /// The end of the area is being captured.
        /// </summary>
        CapturingEnd,

        /// <summary>
        /// Capturing has been completed.
        /// </summary>
        Finished
    }
}
