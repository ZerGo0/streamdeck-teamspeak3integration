document.addEventListener("websocketCreate",
	function() {
		console.log("Websocket created!");

		websocket.addEventListener("message",
			function(event) {
				console.log("Got message event!");

				// Received message from Stream Deck
				var jsonObj = JSON.parse(event.data);

				if (jsonObj.event === "sendToPropertyInspector") {
					var payload = jsonObj.payload;
				} else if (jsonObj.event === "didReceiveSettings") {
					var payload = jsonObj.payload;
				}
			});
	});