"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

const autocompleteList = document.getElementById("autocompleteList");

let contextName = "";
let commands = [
    "help",
    "exit",
    "status",
    "send",
    "connect",
    "disconnect",
    "restart",
    "version",
    "info"
];

// Load the stored user input on page load
document.addEventListener("DOMContentLoaded", function () {
    const savedUserInput = localStorage.getItem("userInput");
    if (savedUserInput) {
        document.getElementById("userInput").value = savedUserInput;
 
        // Clear the input field and refocus
        const messageInput = document.getElementById("messageInput");
        messageInput.value = "";
        messageInput.focus();
    }else{
        const userInput = document.getElementById("userInput");
        userInput.value = "";
        userInput.focus();
    }
});

connection.on("ReceiveLine", function (message) {
    const messageList = document.getElementById("messagesList");

    const li = document.createElement("li");

    // Replace inline color tags with corresponding <span> elements
    // Example: [red]word[/red] -> <span style="color: red;">word</span>
    const htmlMessage = message.replace(/\[([a-zA-Z]+)\](.+?)\[\/\1\]/g, (match, color, text) => {
        // Escape color and text to prevent potential injection shenanigans 
        const safeColor = color.replace(/[^a-zA-Z]/g, "");
        const safeText = text.replace(/</g, "&lt;").replace(/>/g, "&gt;");
        const shadow = `text-shadow: 0 0 10px ${safeColor};`
        return `<span style="color: ${safeColor};${shadow}">${safeText}</span>`;
    });


    li.innerHTML = htmlMessage;
    messageList.appendChild(li);
    ScrollToBottom();
});

connection.on("ReceiveCommandListAndHelp", function (commandList, newContextName) {
    commands = commandList;
    contextName = newContextName;
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

let messageHistory = [];
let historyIndex = -1;

function SendCurrentMessage() {
    const userInput = document.getElementById("userInput");
    const messageInput = document.getElementById("messageInput");
    const user = userInput.value;
    const message = messageInput.value;

    if (message === "") return;

    connection.invoke("PlayerSendCommand", user, message).catch(function (err) {
        return console.error(err.toString());
    });

    localStorage.setItem("userInput", user);

    // Add message to history and reset history index
    messageHistory.push(message);
    historyIndex = -1;

    // Clear the input field and refocus
    messageInput.value = "";
    messageInput.focus();
    hideAutocomplete();
}

document.getElementById("sendButton").addEventListener("click", function (event) {
    SendCurrentMessage();
    event.preventDefault();
});

let currentMatch = "";

document.getElementById("messageInput").addEventListener("keydown", function (event) {
    if (event.key === "Enter") {
        event.preventDefault(); // Prevent the default Enter key action
        SendCurrentMessage();
    } else if (event.key === "ArrowUp") {
        handleHistoryNavigation(-1);
    } else if (event.key === "ArrowDown") {
        handleHistoryNavigation(1);
    } else if (event.key === "Tab" && currentMatch) {
        event.preventDefault();
        // Fill in the command and clear the match
        messageInput.value = currentMatch;
        fillHelpLine();
    }
});

messageInput.addEventListener("input", function () {
    const inputText = messageInput.value.toLowerCase();
    if (!inputText) {
        currentMatch = null;
        fillHelpLine();
        return;
    }

    // Find the singular best match
    const bestMatch = commands.find(cmd => cmd.startsWith(inputText));
    if (bestMatch) {
        currentMatch = bestMatch;
    } else {
        currentMatch = null;
    }
    fillHelpLine();
});

function fillHelpLine() {
    let autocompleteList = document.getElementById("autocompleteList");

    // Clear previous content
    autocompleteList.innerHTML = "";

    var secondPart = currentMatch;
    if(currentMatch == null || currentMatch === ""){
        secondPart = commands;
    }

    // Create elements for the colored text
    const contextSpan = document.createElement("span");
    contextSpan.style.color = "yellow";
    contextSpan.textContent = contextName;

    const dashSpan = document.createElement("span");
    dashSpan.textContent = " : ";

    const matchSpan = document.createElement("span");
    matchSpan.style.color = "magenta";
    matchSpan.textContent = secondPart;

    // Append elements to the autocomplete list
    autocompleteList.appendChild(contextSpan);
    autocompleteList.appendChild(dashSpan);
    autocompleteList.appendChild(matchSpan);
}

// function hideAutocomplete() {
//     currentMatch = "";
//     const autocompleteList = document.getElementById("autocompleteList");
//     if (autocompleteList) {
//         autocompleteList.innerHTML = ""; // Clears all children instead of removing the element
//     }
// }

// // Hide autocomplete list on blur
// messageInput.addEventListener("blur", () => {
//     setTimeout(() => hideAutocomplete(), 100);
// });

function handleHistoryNavigation(direction) {
    const messageInput = document.getElementById("messageInput");

    if (direction === -1 && historyIndex < messageHistory.length - 1) {
        // Move up in history
        historyIndex++;
    } else if (direction === 1 && historyIndex > 0) {
        // Move down in history
        historyIndex--;
    } else if (direction === 1 && historyIndex === 0) {
        // Clear the input field when moving past the most recent history
        historyIndex = -1;
        messageInput.value = "";
        return;
    }

    if (historyIndex >= 0 && historyIndex < messageHistory.length) {
        messageInput.value = messageHistory[messageHistory.length - 1 - historyIndex];
    }
}
