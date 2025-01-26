"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveLine", function (message) {
    const messageList = document.getElementById("messagesList");

    const li = document.createElement("li");

    // Replace inline color tags with corresponding <span> elements
    // Example: [red]word[/red] -> <span style="color: red;">word</span>
    const htmlMessage = message.replace(/\[([a-zA-Z]+)\](.+?)\[\/\1\]/g, (match, color, text) => {
        // Escape color and text to prevent potential injection
        const safeColor = color.replace(/[^a-zA-Z]/g, "");
        const safeText = text.replace(/</g, "&lt;").replace(/>/g, "&gt;");
        const shadow = `text-shadow: 0 0 10px ${safeColor};`
        return `<span style="color: ${safeColor};${shadow}">${safeText}</span>`;
    });

    // Set the HTML content of the list item
    li.innerHTML = htmlMessage;

    messageList.appendChild(li);

    ScrollToBottom();
});


connection.on("ReceiveImage", function (imageUrl) {
    const messageList = document.getElementById("messagesList");

    const li = document.createElement("li");
    const img = document.createElement("img");
    img.src = imageUrl;
    img.alt = "Received image"; // Optional: Alt text for accessibility
    img.style.maxWidth = "100%"; // Optional: Ensures the image fits within the container

    li.appendChild(img);
    messageList.appendChild(li);

    ScrollToBottom();
});

connection.on("PlaySound", function (soundURL) {
    var audio = new Audio(soundURL);
    audio.play();

    ScrollToBottom();
});

function ScrollToBottom(){
    const scrollBox = document.querySelector('.scroll-box');
    scrollBox.scrollTop = scrollBox.scrollHeight;
    
    const messageList = document.getElementById("messagesList");
    messageList.scrollTop = messageList.scrollHeight;
}

// Handle connection closed
connection.onclose(function (error) {
    console.error("Connection closed.");
    if (error) {
        console.error("Error details:", error);
    }
    const messageList = document.getElementById("messagesList");

    const li = document.createElement("li");

    const safeColor = "red"
    const safeText = "I think you got disconnected. You probably need to refresh to reconnect."
    const shadow = `text-shadow: 0 0 10px ${safeColor};`
    htmlMessage = `<span style="color: ${safeColor};${shadow}">${safeText}</span>`;

    // Set the HTML content of the list item
    li.innerHTML = htmlMessage;

    messageList.appendChild(li);

    ScrollToBottom();
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

    connection.invoke("PlayerSendCommand", user, message).catch(function (err) {
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