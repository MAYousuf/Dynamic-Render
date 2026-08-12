/*
 * Drives the single-form request wizard.
 *
 * The form is posted exactly once. Everything between opening the page and submitting it happens
 * here: moving between the three panes, and building the exhibit / evidence / inspection structure
 * that decides which strongly typed model each inspection gets.
 *
 * Steps 1 and 2 are Vue. Step 2's structure is a plain reactive array and each field's `name` is
 * computed from its position in that array, so the contiguous indices model binding needs fall out
 * of the render - there is no prefix rewriting and no reindexing here.
 *
 * Step 3 is not built in the browser at all. The structure is sent to the DataPane handler, which
 * resolves each (evidence type, inspection type) combination server-side and returns the whole pane
 * as rendered Razor. That markup is injected verbatim with v-html and Vue never owns it, so the
 * inputs keep their asp-for names and their unobtrusive-validation attributes and bind straight
 * back on the final post.
 */
(function () {
    'use strict';

    /*
     * The wizard element. Mounting *replaces* this node with the one Vue renders from it, so what is
     * captured here is detached the moment the app mounts: `mounted` repoints this at `vm.$el`, and
     * every DOM lookup below goes through the variable rather than holding on to a node.
     */
    var root = document.getElementById('request-wizard');

    if (!root || typeof Vue === 'undefined') {
        return;
    }

    var configElement = document.querySelector('[data-request-form-config]');
    var config = configElement ? JSON.parse(configElement.textContent) : {};
    var evidenceTypes = config.evidenceTypes || [];
    var inspectionTypesByEvidenceType = config.inspectionTypesByEvidenceType || {};

    // Step 3 as the server rendered it for this response: empty on a fresh GET, and the posted cards
    // when a failed post is being redisplayed. Read before Vue mounts, because mounting replaces the
    // markup it was rendered next to.
    var seed = document.querySelector('[data-pane3-initial]');
    var initialDataPaneHtml = seed ? seed.innerHTML : '';

    if (seed) {
        seed.remove();
    }

    // ---------------------------------------------------------------- helpers

    var keySeed = 0;

    /** Identity for v-for. Without it Vue reuses row DOM by position and typed values shift on remove. */
    function nextKey() {
        return 'row-' + (++keySeed);
    }

    function newId() {
        if (window.crypto && typeof window.crypto.randomUUID === 'function') {
            return window.crypto.randomUUID();
        }

        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = (Math.random() * 16) | 0;
            var v = c === 'x' ? r : ((r & 0x3) | 0x8);
            return v.toString(16);
        });
    }

    function displayNameOf(list, code) {
        for (var i = 0; i < list.length; i++) {
            if (list[i].code === code) {
                return list[i].displayName;
            }
        }

        return code;
    }

    function evidenceTypeName(code) {
        return displayNameOf(evidenceTypes, code);
    }

    function scrollToTop() {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function antiForgeryToken() {
        if (window.abp && abp.security && abp.security.antiForgery) {
            return abp.security.antiForgery.getToken();
        }

        var field = root.querySelector('input[name="__RequestVerificationToken"]');

        return field ? field.value : '';
    }

    // --------------------------------------------------------------- structure

    function newInspection() {
        return { id: newId(), inspectionTypeCode: '' };
    }

    function newEvidence() {
        return { key: nextKey(), evidenceTypeCode: '', description: '', inspections: [] };
    }

    function newExhibit() {
        return { key: nextKey(), description: '', evidences: [newEvidence()] };
    }

    /** Rebuilds the reactive structure from the server's view of it, keys and all. */
    function hydrate(raw) {
        var exhibits = (raw || []).map(function (exhibit) {
            return {
                key: nextKey(),
                description: exhibit.description || '',
                evidences: (exhibit.evidences || []).map(function (evidence) {
                    return {
                        key: nextKey(),
                        evidenceTypeCode: evidence.evidenceTypeCode || '',
                        description: evidence.description || '',
                        inspections: (evidence.inspections || []).map(function (inspection) {
                            return {
                                id: inspection.id || newId(),
                                inspectionTypeCode: inspection.inspectionTypeCode || ''
                            };
                        })
                    };
                })
            };
        });

        return exhibits.length ? exhibits : [newExhibit()];
    }

    // ------------------------------------------------------- inspection data

    var DATA_MARKER = '.Data.';

    /** Turns `Inspections[0].Data.Caliber` into `Caliber`; anything else is not a card value. */
    function cardValueName(name) {
        var at = name.indexOf(DATA_MARKER);

        return at < 0 ? null : name.slice(at + DATA_MARKER.length);
    }

    function cards() {
        return Array.prototype.slice.call(
            root.querySelectorAll('[data-container="inspection-data"] [data-row="data"]'));
    }

    function cardFields(card) {
        return Array.prototype.slice.call(card.querySelectorAll('input, select, textarea'));
    }

    function cardLabel(card) {
        function header(name) {
            var element = card.querySelector('[data-header="' + name + '"]');

            return element ? element.textContent.trim() : '';
        }

        return 'Exhibit #' + header('exhibit') + ' / ' + header('evidence-type') + ' / ' +
            header('inspection-type');
    }

    /**
     * What is currently typed into step 3, keyed by inspection id. The cards themselves are thrown
     * away and re-rendered by the server on every visit to step 3, so this is what survives a trip
     * back to step 2.
     */
    function snapshotCards() {
        var snapshot = {};

        cards().forEach(function (card) {
            var values = {};

            cardFields(card).forEach(function (field) {
                var name = cardValueName(field.name || '');

                if (name) {
                    values[name] = field.value;
                }
            });

            snapshot[card.getAttribute('data-inspection-id')] = {
                discriminator: card.getAttribute('data-discriminator'),
                values: values
            };
        });

        return snapshot;
    }

    /**
     * Reapplies the snapshot to the freshly rendered cards. A card whose combination now resolves to
     * a different model is left empty and called out, rather than the data quietly disappearing.
     */
    function restoreCards(snapshot) {
        var notices = [];

        cards().forEach(function (card) {
            var previous = snapshot[card.getAttribute('data-inspection-id')];

            if (!previous) {
                return;
            }

            if (previous.discriminator !== card.getAttribute('data-discriminator')) {
                notices.push(
                    cardLabel(card) + ': previously entered data was discarded because the ' +
                    'inspection configuration changed.');
                return;
            }

            cardFields(card).forEach(function (field) {
                var name = cardValueName(field.name || '');

                if (name && Object.prototype.hasOwnProperty.call(previous.values, name)) {
                    field.value = previous.values[name];
                }
            });
        });

        return notices;
    }

    // ------------------------------------------------------ conditional fields

    function syncChemicalAnalysis(container) {
        if (!container) {
            return;
        }

        var select = container.querySelector('[data-chemical-result]');
        var block = container.querySelector('[data-positive-only]');

        if (!select || !block) {
            return;
        }

        var selected = select.options[select.selectedIndex];
        block.style.display = selected && selected.text === 'Positive' ? '' : 'none';
    }

    function syncConditionalFields() {
        root.querySelectorAll('[data-chemical-analysis]').forEach(syncChemicalAnalysis);
    }

    // Delegated, because step 3 is replaced wholesale every time it is rendered. Bound in `mounted`,
    // once `root` is the element the listener has to sit on.
    function onFieldChanged(event) {
        var select = event.target.closest ? event.target.closest('[data-chemical-result]') : null;

        if (select) {
            syncChemicalAnalysis(select.closest('[data-chemical-analysis]'));
        }
    }

    // ------------------------------------------------------------ validation

    function form() {
        return root.querySelector('[data-request-form]');
    }

    function reparseValidation() {
        if (!window.jQuery || !window.jQuery.validator || !window.jQuery.validator.unobtrusive) {
            return;
        }

        var $form = window.jQuery(form());
        $form.removeData('validator').removeData('unobtrusiveValidation');
        window.jQuery.validator.unobtrusive.parse($form);
    }

    function validateVisibleFields(scope) {
        if (!window.jQuery || !window.jQuery.fn.valid || !scope) {
            return true;
        }

        var valid = true;

        window.jQuery(scope).find('input, select, textarea').each(function () {
            if (this.name && !window.jQuery(this).valid()) {
                valid = false;
            }
        });

        return valid;
    }

    /** Which pane an element belongs to, or null for anything outside the three panes. */
    function paneOf(element) {
        var section = element && element.closest ? element.closest('[data-pane]') : null;
        var pane = section ? parseInt(section.getAttribute('data-pane'), 10) : NaN;

        return isNaN(pane) ? null : pane;
    }

    // ------------------------------------------------------------- components

    /** ASP.NET turns '[', ']' and '.' into '_' when it generates element ids; labels follow suit. */
    var fieldIdMixin = {
        methods: {
            fieldId: function (name) {
                return (this.path + '.' + name).replace(/[\[\].]/g, '_');
            }
        }
    };

    Vue.component('inspection-row', {
        template: '#inspection-row-template',
        mixins: [fieldIdMixin],
        props: ['inspection', 'evidenceTypeCode', 'path'],
        computed: {
            inspectionTypeOptions: function () {
                return inspectionTypesByEvidenceType[this.evidenceTypeCode] || [];
            }
        }
    });

    Vue.component('evidence-row', {
        template: '#evidence-row-template',
        mixins: [fieldIdMixin],
        props: ['evidence', 'path'],
        computed: {
            evidenceTypeOptions: function () {
                return evidenceTypes;
            }
        },
        methods: {
            addInspection: function () {
                this.evidence.inspections.push(newInspection());
            },

            /**
             * A different evidence type resolves to different inspection models, so the inspections
             * chosen under the old type are no longer valid combinations and go.
             */
            onEvidenceTypeChanged: function () {
                var removed = this.evidence.inspections.splice(0).length;

                if (removed > 0) {
                    this.$root.warn(
                        'Changing an evidence type removed ' + removed + ' inspection(s), because a ' +
                        'different evidence type resolves to different inspection models.');
                }
            }
        }
    });

    Vue.component('exhibit-row', {
        template: '#exhibit-row-template',
        mixins: [fieldIdMixin],
        props: ['exhibit', 'index', 'path'],
        methods: {
            addEvidence: function () {
                this.exhibit.evidences.push(newEvidence());
            }
        }
    });

    // -------------------------------------------------------------- the wizard

    new Vue({
        el: '#request-wizard',

        data: {
            pane: config.activePane || 1,
            exhibits: hydrate(config.exhibits),
            dataPaneHtml: initialDataPaneHtml,
            notices: [],
            warning: '',
            loading: false
        },

        computed: {
            /** The same rules the server enforces in CreateModel.ValidateStructure. */
            structureProblems: function () {
                var problems = [];
                var total = 0;

                if (!this.exhibits.length) {
                    problems.push('Add at least one exhibit.');
                }

                this.exhibits.forEach(function (exhibit, e) {
                    if (!exhibit.evidences.length) {
                        problems.push('Exhibit #' + (e + 1) + ': add at least one evidence.');
                    }

                    exhibit.evidences.forEach(function (evidence) {
                        if (!evidence.evidenceTypeCode) {
                            problems.push(
                                'Exhibit #' + (e + 1) + ': every evidence needs an evidence type.');
                            return;
                        }

                        if (!evidence.inspections.length) {
                            problems.push(
                                'Exhibit #' + (e + 1) + ': evidence needs at least one inspection.');
                        }

                        evidence.inspections.forEach(function (inspection) {
                            if (!inspection.inspectionTypeCode) {
                                problems.push(
                                    'Exhibit #' + (e + 1) + ': every inspection needs an inspection type.');
                                return;
                            }

                            total++;
                        });
                    });
                });

                if (total === 0) {
                    problems.push('Add at least one inspection.');
                }

                return problems.filter(function (problem, index, all) {
                    return all.indexOf(problem) === index;
                });
            }
        },

        methods: {
            stepClass: function (step) {
                if (step === this.pane) {
                    return 'bg-primary text-white';
                }

                return step < this.pane
                    ? 'bg-success-subtle text-success-emphasis'
                    : 'bg-body-secondary text-body-secondary';
            },

            warn: function (message) {
                this.warning = message;
            },

            addExhibit: function () {
                this.warning = '';
                this.exhibits.push(newExhibit());
            },

            removeExhibit: function (index) {
                this.warning = '';
                this.exhibits.splice(index, 1);
            },

            back: function () {
                this.warning = '';

                if (this.pane > 1) {
                    this.pane--;
                    scrollToTop();
                }
            },

            next: function () {
                this.warning = '';

                if (this.pane === 1) {
                    if (!validateVisibleFields(root.querySelector('[data-pane="1"]'))) {
                        return;
                    }

                    this.pane = 2;
                    scrollToTop();
                    return;
                }

                if (this.pane === 2) {
                    var problems = this.structureProblems;

                    if (problems.length) {
                        this.warning = problems.join(' ');
                        return;
                    }

                    this.loadDataPane();
                }
            },

            /**
             * The theme clears jQuery Validate's `ignore`, so submitting validates the panes that are
             * off screen as well. Without this the Submit button would simply do nothing whenever the
             * blocking field is on a pane the user cannot see: show that pane and its first error.
             */
            showPaneWithFirstError: function (validator) {
                var self = this;

                var panes = (validator.errorList || [])
                    .map(function (error) {
                        return paneOf(error.element);
                    })
                    .filter(function (pane) {
                        return pane !== null;
                    });

                if (!panes.length) {
                    return;
                }

                var target = Math.min.apply(null, panes);

                if (target === this.pane) {
                    return;
                }

                this.pane = target;

                this.$nextTick(function () {
                    var first = validator.errorList.filter(function (error) {
                        return paneOf(error.element) === target;
                    })[0];

                    if (first) {
                        first.element.focus();
                    }

                    self.warning = 'Step ' + target + ' still has fields that need attention.';
                    scrollToTop();
                });
            },

            /**
             * Asks the server to render step 3 for the structure as it currently stands. Only the
             * structure goes over the wire; which concrete model and which strongly typed partial
             * each inspection gets is decided server-side, and comes back as markup.
             */
            loadDataPane: function () {
                var self = this;
                var snapshot = snapshotCards();

                self.loading = true;
                self.notices = [];

                fetch(window.location.pathname + '?handler=DataPane', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': antiForgeryToken()
                    },
                    body: JSON.stringify({ exhibits: self.exhibits })
                }).then(function (response) {
                    return response.text().then(function (text) {
                        if (!response.ok) {
                            throw new Error(
                                text || ('Step 3 could not be rendered (' + response.status + ').'));
                        }

                        return text;
                    });
                }).then(function (html) {
                    self.dataPaneHtml = html;
                    self.pane = 3;

                    return self.$nextTick();
                }).then(function () {
                    self.notices = restoreCards(snapshot);
                    syncConditionalFields();
                    reparseValidation();
                    scrollToTop();
                }).catch(function (error) {
                    self.warning = error.message;
                }).then(function () {
                    self.loading = false;
                });
            }
        },

        mounted: function () {
            var self = this;

            // Vue rendered a new element and threw away the one it mounted on, so everything the
            // rest of this file reaches for lives under `$el` from here on - including the form the
            // validator has to be attached to.
            root = this.$el;

            root.addEventListener('change', onFieldChanged);

            // Mounting re-created pane 1's server-rendered markup, so the validator's references to
            // it are stale.
            reparseValidation();
            syncConditionalFields();

            if (window.jQuery) {
                window.jQuery(form()).on('invalid-form.validate', function (event, validator) {
                    self.showPaneWithFirstError(validator);
                });
            }
        }
    });
})();
