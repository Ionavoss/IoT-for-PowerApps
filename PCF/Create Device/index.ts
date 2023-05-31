import { IInputs, IOutputs } from "./generated/ManifestTypes";

export class createdevice implements ComponentFramework.StandardControl<IInputs, IOutputs> {

    private _context: ComponentFramework.Context<IInputs>;
    private _container: HTMLDivElement
    private _textarea: HTMLTextAreaElement
    private _button: HTMLButtonElement

    constructor() {

    }

    public init(context: ComponentFramework.Context<IInputs>, notifyOutputChanged: () => void, state: ComponentFramework.Dictionary, container: HTMLDivElement): void {
        this._context = context
        context.mode.trackContainerResize(true)
        this._container = document.createElement("div")
        this._textarea = document.createElement("textarea")
        this._textarea.setAttribute("class", "IOT_NAME")
        this._textarea.addEventListener("keypress", this.textinput);

        this._button = document.createElement("button")
        this._button.innerText = "Create device";
        this._button.addEventListener("click", this.buttonClick);
        this._button.setAttribute("class", "IOT_CREATE")

        container.appendChild(this._container)
        container.appendChild(this._textarea)
        container.appendChild(this._button)

    }

    private textinput = (event: KeyboardEvent): void => {
        var onlyLettersAndNumbers = /^[a-zA-Z0-9]+$/;
        var isValid = onlyLettersAndNumbers.test(event.key);
        if (!isValid) {
            event.preventDefault()
        }
    }

    private buttonClick = (): void => {
        this.createDevice();
    }

    private async createDevice() {

        const functionCode = this._context.parameters.functionCode.raw
        const functionUri = this._context.parameters.functionBaseURL.raw
        const deviceId = this._textarea.value;

        fetch(`https://${functionUri}.azurewebsites.net/api/devices/${deviceId}/create?code=${functionCode}`,
            {
                method: 'POST',
                mode: 'cors',
                headers: {
                    'Access-Control-Allow-Origin': '*' // Replace * with the actual allowed origin if known
                }
            })
            .then(response => {

                if (response.status === 200) {
                    return response.json()

                }
            })
            .then(data => {
                const area = document.querySelector('textarea');
                if (data.status === "Already Exists") {
                    if (area) {
                        area.value = "Device already exists";
                    }
                } else {
                    if (area) { area.value = "Device created" }
                }
            })
            .catch(e => {
                console.log(e)
                console.log(e.error());
            })


    }


    public updateView(context: ComponentFramework.Context<IInputs>): void {

        const _pix = context.mode.allocatedHeight * 0.5
        const textareaElement = this._textarea;
        if (textareaElement) {
            textareaElement.style.height = `${_pix}px`;
        }
        const button = this._button;
        if (button) {
            button.style.height = `${_pix}px`
        }
    }

    public getOutputs(): IOutputs {
        return {};
    }

    public destroy(): void {
        // Add code to cleanup control if necessary
    }
}
