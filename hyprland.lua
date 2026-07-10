local xivIsFocused = false
-- focus tracking for XivHyprIdle
---@param window HL.Window
hl.on("window.active", function(window)
	if window then
		if window.class == "ffxiv_dx11.exe" then
			xivIsFocused = true
			hl.dispatch(hl.dsp.exec_cmd("bash -c 'echo -n true > /dev/udp/127.0.0.1/15432'"))
		elseif xivIsFocused then
			xivIsFocused = false
			hl.dispatch(hl.dsp.exec_cmd("bash -c 'echo -n false > /dev/udp/127.0.0.1/15432'"))
		end
	elseif xivIsFocused then
		xivIsFocused = false
		hl.dispatch(hl.dsp.exec_cmd("bash -c 'echo -n false > /dev/udp/127.0.0.1/15432'"))
	end
end)
