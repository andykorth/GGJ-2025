"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (user, message) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    li.textContent = `${user} says ${message}`;
});

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

function SendCurrentMessage(){
	var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;
	
	if(message === "") return;

    connection.invoke("SendMessage", user, message).catch(function (err) {
        return console.error(err.toString());
    });
	// clear the field and refocus it.
	document.getElementById("messageInput").value = "";
	document.getElementById("messageInput").focus();
}

document.getElementById("sendButton").addEventListener("click", function (event) {
	SendCurrentMessage();
	event.preventDefault();
});

document.getElementById("messageInput").addEventListener("keydown", function (event) {
    handleEnter(event);
})

function handleEnter(event) {
    if (event.key === "Enter") {
        event.preventDefault(); // Prevent the default Enter key action
		SendCurrentMessage();
    }
}