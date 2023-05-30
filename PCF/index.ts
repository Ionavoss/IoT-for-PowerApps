import {IInputs, IOutputs} from "./generated/ManifestTypes";

// c2d/{deviceId}

export class devicestatus implements ComponentFramework.StandardControl<IInputs, IOutputs> {

    private _context: ComponentFramework.Context<IInputs>;
    private _container: HTMLDivElement;
    private _button: HTMLButtonElement;
    private _textarea: HTMLTextAreaElement;

    /**
     * Empty constructor.
     */
    constructor()
    {

    }

    /**
     * Used to initialize the control instance. Controls can kick off remote server calls and other initialization actions here.
     * Data-set values are not initialized here, use updateView.
     * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to property names defined in the manifest, as well as utility functions.
     * @param notifyOutputChanged A callback method to alert the framework that the control has new outputs ready to be retrieved asynchronously.
     * @param state A piece of data that persists in one session for a single user. Can be set at any point in a controls life cycle by calling 'setControlState' in the Mode interface.
     * @param container If a control is marked control-type='standard', it will receive an empty div element within which it can render its content.
     */
    public init(context: ComponentFramework.Context<IInputs>, notifyOutputChanged: () => void, state: ComponentFramework.Dictionary, container:HTMLDivElement): void
    {
        context.mode.trackContainerResize(true)
        this._context = context;
        this._container = container;
        this._container.id = "id1"

        // Create the button element
        this._button = document.createElement("button");
        this._button.className = "button1";
        this._button.textContent = "Click me";

        this._textarea = document.createElement("textarea");
        this._textarea.className = "textarea1"

        // Add click event listener to the button
        this._button.addEventListener("click", this.buttonClick);
        //this._button.setAttribute("class", "button1")

        // Append the button to the container
        this._container.appendChild(this._textarea);
        this._container.appendChild(this._button);
        //container.appendChild(this._container);
    }

    private buttonClick = (): void => {
        console.log( "Click")
        const textareaText = this._container.querySelector('textarea')
        if( textareaText) {
            if( textareaText.value.length > 0 ){
            this.SendMessage( textareaText.value)
            textareaText.value = ""
            }
        }
    } 

    /**
     * Called when any value in the property bag has changed. This includes field values, data-sets, global values such as container height and width, offline status, control metadata values such as label, visible, etc.
     * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to names defined in the manifest, as well as utility functions
     */
    public updateView(context: ComponentFramework.Context<IInputs>): void {
        console.log(context.mode.allocatedHeight)
        const _pix = context.mode.allocatedHeight * 0.8
        const textareaElement = this._container.querySelector('textarea');
         if (textareaElement) {
            textareaElement.style.height = `${_pix}px`;
        }
        const _pix2 = context.mode.allocatedHeight - _pix
        const button = this._container.querySelector('button');
        if (button) {
            button.style.height = `${_pix2}px`
        }
    }

    private async SendMessage( message: string) {
        const device = this._context.parameters.deviceId.raw;
        const code = this._context.parameters.FunctionsCode.raw;
        const url = this._context.parameters.FunctionsUrl.raw;
        fetch( `https://${url}.azurewebsites.net/api/c2d/${device}?code=${code}`,
        {
            method: 'POST',
            mode: 'cors',
            headers: {
              'Access-Control-Allow-Origin': '*' // Replace * with the actual allowed origin if known
            },
            body: `
            {"message": "${message}"}
            `
          })
        .then(response => {
            console.log(response)

        })
        .catch(error => {
          console.error('Error:', error);
        })
    }
    

    /**
     * It is called by the framework prior to a control receiving new data.
     * @returns an object based on nomenclature defined in manifest, expecting object[s] for property marked as “bound” or “output”
     */
    public getOutputs(): IOutputs
    {
        return {};
    }

    /**
     * Called when the control is to be removed from the DOM tree. Controls should use this call for cleanup.
     * i.e. cancelling any pending remote calls, removing listeners, etc.
     */
    public destroy(): void
    {
        // Add code to cleanup control if necessary
    }
}